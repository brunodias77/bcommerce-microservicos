using FluentMigrator;

namespace UserService.Infrastructure.Migrations
{
    [Migration(20241201003)]
    public class CreateUsersTable : Migration
    {
        public override void Up()
        {
            Create.Table("users")
                .WithColumn("user_id").AsGuid().PrimaryKey()
                .WithColumn("keycloak_id").AsGuid().Nullable().Unique()
                .WithColumn("first_name").AsString(100).NotNullable()
                .WithColumn("last_name").AsString(155).NotNullable()
                .WithColumn("email").AsString(255).NotNullable().Unique()
                .WithColumn("email_verified_at").AsDateTimeOffset().Nullable()
                .WithColumn("phone").AsString(20).Nullable()
                .WithColumn("password_hash").AsString(255).Nullable()
                .WithColumn("cpf").AsFixedLengthString(11).Nullable().Unique()
                .WithColumn("date_of_birth").AsDate().Nullable()
                .WithColumn("newsletter_opt_in").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("ativo")
                .WithColumn("role").AsCustom("user_role_enum").NotNullable()
                .WithColumn("failed_login_attempts").AsInt16().NotNullable().WithDefaultValue(0)
                .WithColumn("account_locked_until").AsDateTimeOffset().Nullable()
                .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("deleted_at").AsDateTimeOffset().Nullable()
                .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

            // Adicionar valores padrão usando SQL
            Execute.Sql("ALTER TABLE users ALTER COLUMN user_id SET DEFAULT gen_random_uuid();");
            Execute.Sql("ALTER TABLE users ALTER COLUMN role SET DEFAULT 'customer';");

            // Adicionar constraints
            Execute.Sql("ALTER TABLE users ADD CONSTRAINT chk_phone_format CHECK (phone IS NULL OR phone ~ '^\\+?[1-9]\\d{1,14}$');");
            Execute.Sql("ALTER TABLE users ADD CONSTRAINT chk_status_values CHECK (status IN ('ativo', 'inativo', 'banido'));");
            Execute.Sql("ALTER TABLE users ADD CONSTRAINT chk_cpf_valid CHECK (cpf IS NULL OR is_cpf_valid(cpf));");
            Execute.Sql("ALTER TABLE users ADD CONSTRAINT chk_auth_method CHECK (password_hash IS NOT NULL OR keycloak_id IS NOT NULL);");

            // Criar índices usando SQL direto para suportar WHERE clause
            Execute.Sql("CREATE INDEX idx_users_active_email ON users (email) WHERE deleted_at IS NULL;");
            Execute.Sql("CREATE INDEX idx_users_status ON users (status) WHERE deleted_at IS NULL;");

            Create.Index("idx_users_role")
                .OnTable("users")
                .OnColumn("role");

            // Criar trigger para updated_at
            Execute.Sql("CREATE TRIGGER set_timestamp_users BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();");
        }

        public override void Down()
        {
            // Remover trigger
            Execute.Sql("DROP TRIGGER IF EXISTS set_timestamp_users ON users;");

            // Remover tabela
            Delete.Table("users");
        }
    }
}