namespace PassionProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ModifyPropertyListingWithColumnDataType : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.PropertyListings", "Name", c => c.String());
            AlterColumn("dbo.PropertyListings", "Description", c => c.String());
            AlterColumn("dbo.PropertyListings", "Status", c => c.String());
            AlterColumn("dbo.PropertyListings", "Type", c => c.String());
            AlterColumn("dbo.PropertyListings", "UserId", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.PropertyListings", "Type", c => c.String(nullable: false));
            AlterColumn("dbo.PropertyListings", "Status", c => c.String(nullable: false));
            AlterColumn("dbo.PropertyListings", "Description", c => c.String(nullable: false));
            AlterColumn("dbo.PropertyListings", "Name", c => c.String(nullable: false));
            DropColumn("dbo.PropertyListings", "Features");
        }
    }
}
