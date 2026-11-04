using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cashly.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentLogTransactionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Assicura che i valori dell'enum postgres 'transfer_out' e 'transfer_in' siano presenti
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_enum WHERE enumlabel = 'transfer_out' AND enumtypid = 'transaction_type'::regtype) THEN
                        ALTER TYPE transaction_type ADD VALUE 'transfer_out';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_enum WHERE enumlabel = 'transfer_in' AND enumtypid = 'transaction_type'::regtype) THEN
                        ALTER TYPE transaction_type ADD VALUE 'transfer_in';
                    END IF;
                END $$;
            ");

            // 2. Aggiunge la colonna TransactionId se non esiste già
            migrationBuilder.Sql(@"
                ALTER TABLE ""InvestmentLogs"" ADD COLUMN IF NOT EXISTS ""TransactionId"" integer;
            ");

            // 3. Crea l'indice per TransactionId se non esiste
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_InvestmentLogs_TransactionId"" ON ""InvestmentLogs"" (""TransactionId"");
            ");

            // 4. Crea il vincolo di foreign key con ON DELETE RESTRICT se non esiste
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name IN ('FK_InvestmentLogs_Transactions_TransactionId', 'InvestmentLogs_TransactionId_fkey')
                        AND table_name = 'InvestmentLogs'
                    ) THEN
                        ALTER TABLE ""InvestmentLogs"" 
                            ADD CONSTRAINT ""FK_InvestmentLogs_Transactions_TransactionId"" 
                            FOREIGN KEY (""TransactionId"") REFERENCES ""Transactions"" (""TransactionId"") 
                            ON DELETE RESTRICT;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvestmentLogs_Transactions_TransactionId",
                table: "InvestmentLogs");

            migrationBuilder.DropIndex(
                name: "IX_InvestmentLogs_TransactionId",
                table: "InvestmentLogs");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "InvestmentLogs");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:transaction_type", "income,expense")
                .OldAnnotation("Npgsql:Enum:transaction_type", "income,expense,transfer_out,transfer_in");
        }
    }
}
