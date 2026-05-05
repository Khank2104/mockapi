using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlServer("Server=localhost;Database=MotelManagementDB;Trusted_Connection=True;TrustServerCertificate=True;");
using var db = new ApplicationDbContext(optionsBuilder.Options);

var details = db.InvoiceDetails.ToList();
db.InvoiceDetails.RemoveRange(details);
var invoices = db.Invoices.ToList();
db.Invoices.RemoveRange(invoices);
db.SaveChanges();
Console.WriteLine("Deleted all invoices and details.");
