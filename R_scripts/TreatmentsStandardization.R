#paste file name from OmicIntegrator
OmicIntegrator_standardization_file <- "C:/Users/Iván/Documents/Doctorado/OmicIntegrator/Standardization/Etiolated seedlings.xlsx"

library(readxl)
library(dplyr)
library(data.table)
library(writexl)

options(scipen = 999)

path <- dirname(OmicIntegrator_standardization_file)
file_name <- tools::file_path_sans_ext(basename(OmicIntegrator_standardization_file))

treatment_values <- read_xlsx(OmicIntegrator_standardization_file)

#transcriptomes
mean_tpms <- treatment_values %>% 
  filter(SampleType == "Transcriptome") %>% 
  mutate(has_value = if_else(!is.na(AveragedValue), 1, 0)) %>% 
  group_by(FeatureId) %>% 
  summarise(in_datasets = sum(has_value),
            mean_tpm = mean(AveragedValue, na.rm = T)) %>% 
  filter(in_datasets == 4)##only transcripts with data in the four transcriptomes

tpms_fitting <- treatment_values %>% 
  filter(SampleType == "Transcriptome") %>% 
  inner_join(mean_tpms %>% select(FeatureId, mean_tpm))

tpm_regressions <- data.table(TreatmentId = numeric(),
                              Slope = numeric(),
                              Intercept = numeric(),
                              Fit = numeric(),
                              Obs = numeric())

for (trt in unique(tpms_fitting$TreatmentId))
{
  filtered <- tpms_fitting %>% 
    filter(TreatmentId == trt &
             AveragedValue != 0 & 
             mean_tpm != 0)
  
  a <- lm(formula = log(mean_tpm) ~ log(AveragedValue), 
          data = filtered)
  
  tpm_regressions <- tpm_regressions %>% rbind(list(TreatmentId = trt,
                                                    Slope = as.numeric(a$coefficients[2]),
                                                    Intercept = as.numeric(a$coefficients[1]),
                                                    Fit = summary(a)$r.squared,
                                                    Obs = nrow(filtered)),
                                               fill = TRUE)
}

apply_regression_tpm <- function(val, slp, itr) exp(log(val) * slp + itr)

s_treatments <- tpms_fitting %>% 
  inner_join(tpm_regressions) %>% 
  mutate(SampleType = "Transcriptome",
         `S-TPM` = apply_regression_tpm(AveragedValue, Slope, Intercept),
         WithExclusivity = NA) %>% 
  select(TreatmentId, SampleType, FeatureId, `S-value` = `S-TPM`, WithExclusivity)

sa_sample_types <- s_treatments %>% 
  filter(SampleType == "Transcriptome") %>% 
  group_by(FeatureId) %>% 
  summarise(`SA-value` = mean(`S-value`, na.rm = F),
            `S-value_sd` = sd(`S-value`, na.rm = F),
            greater_5 = sum(if_else(!is.na(`S-value`) & `S-value` > 5, 1, 0)),
            less_1 = sum(if_else(!is.na(`S-value`) & `S-value` < 1, 1, 0))) %>% 
  mutate(SampleType = "Transcriptome",
         Presence = if_else(greater_5 >= 3, 
                            "Present",
                            if_else(less_1 >= 3, "Absent", "Uncertain")),
         HighAbundance = Presence == "Present" & `SA-value` > 50) %>%
  select(SampleType, FeatureId, `SA-value`, `S-value_sd`, Presence, HighAbundance)

#proteomes
prot_fitting <- treatment_values %>% 
  filter(SampleType == "Proteome") %>% 
  left_join(sa_sample_types %>% 
              filter(SampleType == "Transcriptome") %>%
              select(FeatureId, 
                     `SA-TPM`= `SA-value`,
                     Transcript_presence = Presence))

prot_regressions <- data.table(TreatmentId = numeric(),
                               Slope = numeric(),
                               Intercept = numeric(),
                               Fit = numeric(),
                               Obs = numeric())

for (trt in unique(prot_fitting$TreatmentId))
{
  filtered <- prot_fitting %>% 
    filter(TreatmentId == trt
           & AveragedValue != 0
           & coalesce(Transcript_presence, "Absent") == "Present")
  
  a <- lm(formula = log(`SA-TPM`) ~ log(AveragedValue), 
          data = filtered)
  
  prot_regressions <- prot_regressions %>%
    rbind(list(TreatmentId = trt,
               Slope = as.numeric(a$coefficients[2]),
               Intercept = as.numeric(a$coefficients[1]),
               Fit = summary(a)$r.squared,
               Obs = nrow(filtered)),
          fill = TRUE)
}

apply_regression_prot <- function(val, slp, itr) exp(log(val) * slp + itr)

s_treatments <- s_treatments %>% 
  rbind(prot_fitting %>% 
          inner_join(prot_regressions) %>% 
          mutate(SampleType = "Proteome",
                 `S-prot` = apply_regression_prot(AveragedValue, Slope, Intercept),
                 WithExclusivity = ExclusivePeptides > 0) %>% 
          select(TreatmentId, SampleType, FeatureId, `S-value` = `S-prot`, WithExclusivity),
        fill = T)

features_with_exclusive_peptides <- unique(
  c((s_treatments %>%
       filter(SampleType == "Proteome"
              & coalesce(`S-value`, 0) > 0
              & WithExclusivity))$FeatureId,
    (treatment_values %>% 
       filter(SampleType == "Phosphoproteome"
              & ExclusivePeptides > 0))$FeatureId))

num_prot_treatments <- length(unique((s_treatments %>% filter(SampleType == "Proteome"))$TreatmentId))

sa_sample_types <- sa_sample_types %>% 
  rbind(s_treatments %>% 
          filter(SampleType == "Proteome") %>% 
          group_by(FeatureId) %>% 
          summarise(`SA-value` = sum(`S-value`, na.rm = T) / num_prot_treatments,
                    `S-value_sd` = sd(`S-value`, na.rm = T),
                    num_samples = sum(if_else(coalesce(`S-value`, 0) > 0, 1, 0))) %>% 
          mutate(SampleType = "Proteome",
                 Presence = if_else(num_samples >= 2
                                    & FeatureId %in% features_with_exclusive_peptides,
                                    "Present",
                                    if_else(num_samples == 0,
                                            "Absent", 
                                            "Uncertain")),
                 HighAbundance = Presence == "Present" & `SA-value` > 50) %>% 
          select(SampleType, FeatureId, `SA-value`, `S-value_sd`, Presence, HighAbundance))


write_xlsx(list(SampleTypes = sa_sample_types, Treatments = s_treatments),
           path = file.path(path, paste0(file_name, "_standardized.xlsx")))
