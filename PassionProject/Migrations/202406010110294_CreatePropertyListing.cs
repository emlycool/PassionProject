namespace PassionProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreatePropertyListing : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PropertyListings",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Slug = c.String(),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        NoBedRooms = c.Int(nullable: false),
                        NoBathRooms = c.Int(nullable: false),
                        SquareFootage = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Description = c.String(),
                        Status = c.String(),
                        Type = c.String(),
                        features = c.String(),
                        UserId = c.Int(nullable: false),
                        PublishedAt = c.DateTime(),
                        CreatedAt = c.DateTime(),
                        UpdatedAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.PropertyListings");
        }
    }
}
