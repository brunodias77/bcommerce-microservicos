using FluentMigrator;

namespace UserService.Infrastructure.Migrations;

/// <summary>
/// Migration para criar a tabela user_addresses
/// </summary>
[Migration(20241220002)]
public class CreateUserAddressesTable : Migration
{
    public override void Up()
    {
        Create.Table("user_addresses")
            .WithColumn("address_id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("postal_code").AsString(8).NotNullable()
            .WithColumn("street").AsString(150).NotNullable()
            .WithColumn("street_number").AsString(20).NotNullable()
            .WithColumn("complement").AsString(100).Nullable()
            .WithColumn("neighborhood").AsString(100).NotNullable()
            .WithColumn("city").AsString(100).NotNullable()
            .WithColumn("state_code").AsString(2).NotNullable()
            .WithColumn("country_code").AsString(2).NotNullable().WithDefaultValue("BR")
            .WithColumn("type").AsString(20).NotNullable()
            .WithColumn("is_default").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable()
            .WithColumn("deleted_at").AsDateTime().Nullable()
            .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

        // Criar chave estrangeira
        Create.ForeignKey("FK_user_addresses_users")
            .FromTable("user_addresses").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.Cascade);

        // Criar índices com filtros condicionais
        Create.Index("idx_user_addresses_user_id")
            .OnTable("user_addresses")
            .OnColumn("user_id");

        // Índice único composto para garantir apenas um endereço padrão por usuário e tipo
        Execute.Sql(@"CREATE UNIQUE INDEX uq_user_addresses_default_per_user_type ON user_addresses (user_id, type) WHERE is_default = TRUE AND deleted_at IS NULL;");

        Create.Index("IX_user_addresses_postal_code")
            .OnTable("user_addresses")
            .OnColumn("postal_code");

        Create.Index("IX_user_addresses_type")
            .OnTable("user_addresses")
            .OnColumn("type");
    }

    public override void Down()
    {
        Delete.Table("user_addresses");
    }
}