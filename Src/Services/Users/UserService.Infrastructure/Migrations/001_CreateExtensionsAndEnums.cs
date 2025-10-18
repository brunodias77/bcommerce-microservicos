using FluentMigrator;

namespace UserService.Infrastructure.Migrations
{
    [Migration(20241201001)]
    public class CreateExtensionsAndEnums : Migration
    {
        public override void Up()
        {
            // Criar extensão pgcrypto
            Execute.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            // Criar enums
            Execute.Sql("CREATE TYPE user_role_enum AS ENUM ('customer', 'admin');");
            Execute.Sql("CREATE TYPE consent_type_enum AS ENUM ('marketing_email', 'newsletter_subscription', 'terms_of_service', 'privacy_policy', 'cookies_essential', 'cookies_analytics', 'cookies_marketing');");
            Execute.Sql("CREATE TYPE card_brand_enum AS ENUM ('visa', 'mastercard', 'amex', 'elo', 'hipercard', 'diners_club', 'discover', 'jcb', 'aura', 'other');");
            Execute.Sql("CREATE TYPE address_type_enum AS ENUM ('shipping', 'billing');");
            Execute.Sql("CREATE TYPE user_token_type_enum AS ENUM ('refresh', 'email_verification', 'password_reset');");
        }

        public override void Down()
        {
            // Remover enums (ordem inversa)
            Execute.Sql("DROP TYPE IF EXISTS user_token_type_enum;");
            Execute.Sql("DROP TYPE IF EXISTS address_type_enum;");
            Execute.Sql("DROP TYPE IF EXISTS card_brand_enum;");
            Execute.Sql("DROP TYPE IF EXISTS consent_type_enum;");
            Execute.Sql("DROP TYPE IF EXISTS user_role_enum;");

            // Remover extensão
            Execute.Sql("DROP EXTENSION IF EXISTS pgcrypto;");
        }
    }
}