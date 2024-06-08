namespace PassionProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Media1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Media", "PropertyListingId", c => c.Int(nullable: false));
            CreateIndex("dbo.Media", "PropertyListingId");
            AddForeignKey("dbo.Media", "PropertyListingId", "dbo.PropertyListings", "Id", cascadeDelete: true);
            DropColumn("dbo.Media", "EntityTypeId");
            DropColumn("dbo.Media", "EntityType");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Media", "EntityType", c => c.String());
            AddColumn("dbo.Media", "EntityTypeId", c => c.Int(nullable: false));
            DropForeignKey("dbo.Media", "PropertyListingId", "dbo.PropertyListings");
            DropIndex("dbo.Media", new[] { "PropertyListingId" });
            DropColumn("dbo.Media", "PropertyListingId");
        }
    }
}
