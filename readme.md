Overview
This repository contains the source code and datasets used to reproduce the results from the manuscript:
“Multi-Omics Meta-Analysis Provides Insights into Reversible Phosphorylation During Arabidopsis Skotomorphogenesis.”

Description
The project includes a C# program and a set of R scripts designed for multi-omics data integration and analysis.
Data from transcriptomic, proteomic, and phosphoproteomic experiments are organized in an SQLite database, enabling flexible and reproducible workflows.

Features
Implemented in C#
•	Data integration: Aggregates data in an SQLite relational database.
•	Dataset management: Supports multiple genome annotations and RNAseq, proteomic, and phosphoproteomic datasets that can be grouped into independent pools.
•	Genome annotation: Loads GFF and FASTA files to import gene models and sequences.
•	Protein translation: Translates CDS annotations into protein sequences, which can be exported as .fasta files.
•	GO annotations: Loads GO term relationships and annotations. The list of genes annotated with any GO term ID can be retrieved.
•	Transcriptomic data: Loads expression matrices (gene IDs with read counts, RPKM, or TPM values) and handles multiple experiments and replicates.
•	Proteomic data: When peptide sequences are provided, exact sequence matching is performed against protein sequences; otherwise, mapping is based on gene IDs.
•	Phosphoproteomic data: Supports different formats for specifying residue positions.
•	External tool outputs: Accumulates results from ScanProSite, PrDOS, and TMHMM.
•	Regex matching: Performs regular expression searches in protein sequences (e.g., to identify kinase recognition motifs).

Implemented in R
•	Data normalization: Performs linear regressions to standardize and average transcript and protein abundance values across datasets.
•	Visualization: Generates scatter plots, density plots, heatmaps, and protein domain diagrams with phosphorylation sites using ggplot2.
•	Phylogenetic analysis: Visualizes phylogenetic trees (from .nwk files) using ggtree.
