using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmicIntegrator.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Genomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genomes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoTerms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Namespace = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoTerms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Motifs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Program = table.Column<string>(type: "TEXT", nullable: true),
                    Code = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalSpecies = table.Column<string>(type: "TEXT", nullable: true),
                    Alias = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Sequence = table.Column<string>(type: "TEXT", nullable: true),
                    Group = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Motifs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentsStandardizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    GenomeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentsStandardizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Datasets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GenomeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Link = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datasets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Datasets_Genomes_GenomeId",
                        column: x => x.GenomeId,
                        principalTable: "Genomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GenomeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", nullable: true),
                    Start = table.Column<long>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Length = table.Column<long>(type: "INTEGER", nullable: false),
                    IsChromosome = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sequences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sequences_Genomes_GenomeId",
                        column: x => x.GenomeId,
                        principalTable: "Genomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoTermsRelactinships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReferenceId = table.Column<string>(type: "TEXT", nullable: false),
                    ReferredId = table.Column<string>(type: "TEXT", nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoTermsRelactinships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoTermsRelactinships_GoTerms_ReferenceId",
                        column: x => x.ReferenceId,
                        principalTable: "GoTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoTermsRelactinships_GoTerms_ReferredId",
                        column: x => x.ReferredId,
                        principalTable: "GoTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProteomesPeptides",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DatasetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sequence = table.Column<string>(type: "TEXT", nullable: true),
                    IdInDataSet = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProteomesPeptides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProteomesPeptides_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Treatments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DatasetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Treatments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Treatments_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: true),
                    SequenceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: true),
                    Alias = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Start = table.Column<long>(type: "INTEGER", nullable: false),
                    End = table.Column<long>(type: "INTEGER", nullable: false),
                    Strand = table.Column<char>(type: "TEXT", nullable: true),
                    Phase = table.Column<int>(type: "INTEGER", nullable: true),
                    IsGeneRepresentative = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Features_Sequences_SequenceId",
                        column: x => x.SequenceId,
                        principalTable: "Sequences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProteomesPeptidesModifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PeptideId = table.Column<long>(type: "INTEGER", nullable: false),
                    ModificationType = table.Column<string>(type: "TEXT", nullable: false),
                    ResiduePosition = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProteomesPeptidesModifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProteomesPeptidesModifications_ProteomesPeptides_PeptideId",
                        column: x => x.PeptideId,
                        principalTable: "ProteomesPeptides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Samples",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TreatmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    IntensityThreshold = table.Column<decimal>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Samples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Samples_Treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "Treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeaturesGoTerms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    GoTermId = table.Column<string>(type: "TEXT", nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturesGoTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeaturesGoTerms_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeaturesGoTerms_GoTerms_GoTermId",
                        column: x => x.GoTermId,
                        principalTable: "GoTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeaturesMotifs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    MotifId = table.Column<long>(type: "INTEGER", nullable: false),
                    Start = table.Column<int>(type: "INTEGER", nullable: false),
                    End = table.Column<int>(type: "INTEGER", nullable: false),
                    Strand = table.Column<char>(type: "TEXT", nullable: false, defaultValue: '+'),
                    PValue = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturesMotifs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeaturesMotifs_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeaturesMotifs_Motifs_MotifId",
                        column: x => x.MotifId,
                        principalTable: "Motifs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeaturesParents",
                columns: table => new
                {
                    FeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturesParents", x => new { x.FeatureId, x.ParentId });
                    table.ForeignKey(
                        name: "FK_FeaturesParents_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeaturesParents_Features_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProteomesPeptidesFeatures",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PeptideId = table.Column<long>(type: "INTEGER", nullable: false),
                    FeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProteomesPeptidesFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProteomesPeptidesFeatures_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProteomesPeptidesFeatures_ProteomesPeptides_PeptideId",
                        column: x => x.PeptideId,
                        principalTable: "ProteomesPeptides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "S_TreatmentValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StandardizationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TreatmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    FeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    SValue = table.Column<decimal>(type: "TEXT", nullable: true),
                    WithExclusivity = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_S_TreatmentValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_S_TreatmentValues_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_S_TreatmentValues_TreatmentsStandardizations_StandardizationId",
                        column: x => x.StandardizationId,
                        principalTable: "TreatmentsStandardizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_S_TreatmentValues_Treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "Treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SA_SampleTypeValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StandardizationId = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleType = table.Column<int>(type: "INTEGER", nullable: false),
                    FeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    SAValue = table.Column<decimal>(type: "TEXT", nullable: true),
                    SValue_SD = table.Column<decimal>(type: "TEXT", nullable: true),
                    Presence = table.Column<int>(type: "INTEGER", nullable: false),
                    HighAbundance = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SA_SampleTypeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SA_SampleTypeValues_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SA_SampleTypeValues_TreatmentsStandardizations_StandardizationId",
                        column: x => x.StandardizationId,
                        principalTable: "TreatmentsStandardizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SummarizedPhosphosites",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StandardizationId = table.Column<int>(type: "INTEGER", nullable: false),
                    FeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    ResiduePosition = table.Column<int>(type: "INTEGER", nullable: false),
                    Residue = table.Column<char>(type: "TEXT", nullable: false),
                    TreatmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExclusivePeptides = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SummarizedPhosphosites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SummarizedPhosphosites_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SummarizedPhosphosites_TreatmentsStandardizations_StandardizationId",
                        column: x => x.StandardizationId,
                        principalTable: "TreatmentsStandardizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SummarizedPhosphosites_Treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "Treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProteomesValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SampleId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeptideId = table.Column<long>(type: "INTEGER", nullable: false),
                    Intensity = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProteomesValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProteomesValues_ProteomesPeptides_PeptideId",
                        column: x => x.PeptideId,
                        principalTable: "ProteomesPeptides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProteomesValues_Samples_SampleId",
                        column: x => x.SampleId,
                        principalTable: "Samples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RnaValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SampleId = table.Column<int>(type: "INTEGER", nullable: false),
                    FeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    Tpm = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RnaValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RnaValues_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RnaValues_Samples_SampleId",
                        column: x => x.SampleId,
                        principalTable: "Samples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_GenomeId",
                table: "Datasets",
                column: "GenomeId");

            migrationBuilder.CreateIndex(
                name: "IX_Features_Code",
                table: "Features",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Features_SequenceId",
                table: "Features",
                column: "SequenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Features_Start",
                table: "Features",
                column: "Start");

            migrationBuilder.CreateIndex(
                name: "IX_Features_Type",
                table: "Features",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturesGoTerms_FeatureId",
                table: "FeaturesGoTerms",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturesGoTerms_GoTermId",
                table: "FeaturesGoTerms",
                column: "GoTermId");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturesGoTerms_Source",
                table: "FeaturesGoTerms",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturesMotifs_FeatureId",
                table: "FeaturesMotifs",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturesMotifs_MotifId",
                table: "FeaturesMotifs",
                column: "MotifId");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturesMotifs_PValue",
                table: "FeaturesMotifs",
                column: "PValue");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturesParents_ParentId",
                table: "FeaturesParents",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_GoTermsRelactinships_ReferenceId",
                table: "GoTermsRelactinships",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_GoTermsRelactinships_ReferredId",
                table: "GoTermsRelactinships",
                column: "ReferredId");

            migrationBuilder.CreateIndex(
                name: "IX_GoTermsRelactinships_Relationship",
                table: "GoTermsRelactinships",
                column: "Relationship");

            migrationBuilder.CreateIndex(
                name: "IX_Motifs_Program_Code",
                table: "Motifs",
                columns: new[] { "Program", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProteomesPeptides_DatasetId",
                table: "ProteomesPeptides",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProteomesPeptidesFeatures_FeatureId",
                table: "ProteomesPeptidesFeatures",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_ProteomesPeptidesFeatures_PeptideId",
                table: "ProteomesPeptidesFeatures",
                column: "PeptideId");

            migrationBuilder.CreateIndex(
                name: "IX_ProteomesPeptidesModifications_PeptideId",
                table: "ProteomesPeptidesModifications",
                column: "PeptideId");

            migrationBuilder.CreateIndex(
                name: "IX_ProteomesValues_PeptideId",
                table: "ProteomesValues",
                column: "PeptideId");

            migrationBuilder.CreateIndex(
                name: "IX_ProteomesValues_SampleId",
                table: "ProteomesValues",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_RnaValues_FeatureId",
                table: "RnaValues",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_RnaValues_SampleId",
                table: "RnaValues",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_S_TreatmentValues_FeatureId",
                table: "S_TreatmentValues",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_S_TreatmentValues_StandardizationId",
                table: "S_TreatmentValues",
                column: "StandardizationId");

            migrationBuilder.CreateIndex(
                name: "IX_S_TreatmentValues_TreatmentId",
                table: "S_TreatmentValues",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SA_SampleTypeValues_FeatureId",
                table: "SA_SampleTypeValues",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_SA_SampleTypeValues_StandardizationId",
                table: "SA_SampleTypeValues",
                column: "StandardizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_TreatmentId",
                table: "Samples",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Sequences_GenomeId",
                table: "Sequences",
                column: "GenomeId");

            migrationBuilder.CreateIndex(
                name: "IX_SummarizedPhosphosites_FeatureId",
                table: "SummarizedPhosphosites",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_SummarizedPhosphosites_StandardizationId",
                table: "SummarizedPhosphosites",
                column: "StandardizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SummarizedPhosphosites_TreatmentId",
                table: "SummarizedPhosphosites",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Treatments_DatasetId",
                table: "Treatments",
                column: "DatasetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeaturesGoTerms");

            migrationBuilder.DropTable(
                name: "FeaturesMotifs");

            migrationBuilder.DropTable(
                name: "FeaturesParents");

            migrationBuilder.DropTable(
                name: "GoTermsRelactinships");

            migrationBuilder.DropTable(
                name: "ProteomesPeptidesFeatures");

            migrationBuilder.DropTable(
                name: "ProteomesPeptidesModifications");

            migrationBuilder.DropTable(
                name: "ProteomesValues");

            migrationBuilder.DropTable(
                name: "RnaValues");

            migrationBuilder.DropTable(
                name: "S_TreatmentValues");

            migrationBuilder.DropTable(
                name: "SA_SampleTypeValues");

            migrationBuilder.DropTable(
                name: "SummarizedPhosphosites");

            migrationBuilder.DropTable(
                name: "Motifs");

            migrationBuilder.DropTable(
                name: "GoTerms");

            migrationBuilder.DropTable(
                name: "ProteomesPeptides");

            migrationBuilder.DropTable(
                name: "Samples");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "TreatmentsStandardizations");

            migrationBuilder.DropTable(
                name: "Treatments");

            migrationBuilder.DropTable(
                name: "Sequences");

            migrationBuilder.DropTable(
                name: "Datasets");

            migrationBuilder.DropTable(
                name: "Genomes");
        }
    }
}
