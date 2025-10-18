using FluentMigrator;

namespace UserService.Infrastructure.Migrations
{
    [Migration(20241201002)]
    public class CreateFunctions : Migration
    {
        public override void Up()
        {
            // Função de timestamp
            Execute.Sql(@"
                CREATE OR REPLACE FUNCTION trigger_set_timestamp()
                RETURNS TRIGGER AS $$
                BEGIN
                  NEW.updated_at = CURRENT_TIMESTAMP;
                  RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Função de validação CPF
            Execute.Sql(@"
                CREATE OR REPLACE FUNCTION is_cpf_valid(cpf TEXT)
                RETURNS BOOLEAN AS $$
                DECLARE
                    cpf_clean TEXT;
                    cpf_array INT[];
                    sum1 INT := 0;
                    sum2 INT := 0;
                    i INT;
                BEGIN
                    cpf_clean := REGEXP_REPLACE(cpf, '[^0-9]', '', 'g');
                    IF LENGTH(cpf_clean) != 11 OR cpf_clean ~ '(\d)\1{10}' THEN
                        RETURN FALSE;
                    END IF;
                    cpf_array := STRING_TO_ARRAY(cpf_clean, NULL)::INT[];
                    FOR i IN 1..9 LOOP
                        sum1 := sum1 + cpf_array[i] * (11 - i);
                    END LOOP;
                    sum1 := 11 - (sum1 % 11);
                    IF sum1 >= 10 THEN sum1 := 0; END IF;
                    IF sum1 != cpf_array[10] THEN RETURN FALSE; END IF;

                    FOR i IN 1..10 LOOP
                        sum2 := sum2 + cpf_array[i] * (12 - i);
                    END LOOP;
                    sum2 := 11 - (sum2 % 11);
                    IF sum2 >= 10 THEN sum2 := 0; END IF;
                    IF sum2 != cpf_array[11] THEN RETURN FALSE; END IF;

                    RETURN TRUE;
                END;
                $$ LANGUAGE plpgsql IMMUTABLE;
            ");
        }

        public override void Down()
        {
            // Remover funções
            Execute.Sql("DROP FUNCTION IF EXISTS is_cpf_valid(TEXT);");
            Execute.Sql("DROP FUNCTION IF EXISTS trigger_set_timestamp();");
        }
    }
}