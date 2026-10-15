using VexCash.SmsService.Models;

namespace VexCash.SmsService.Data
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            if (!context.Templates.Any())
            {
                context.Templates.AddRange(
                    new MessageTemplate
                    {
                        Type = TemplateType.Erinnerung,
                        Title = "Zahlungserinnerung",
                        Content = "{Creditnumber}\n\nSehr geehrte/r {Name}\nBitte zahlen sie bis zum {dueDate}\nDie Summe {amount} zurück",
                        UpdatedAt = DateTime.UtcNow
                    },
                    new MessageTemplate
                    {
                        Type = TemplateType.Mahnung,
                        Title = "1. Mahnung",
                        Content = "Mahnung: Kredit {Creditnumber}\n\nSehr geehrte/r {Name},\ndie Rate in Höhe von {amount} war am {dueDate} fällig. Bitte begleichen Sie den Betrag umgehend.",
                        UpdatedAt = DateTime.UtcNow
                    },
                    new MessageTemplate
                    {
                        Type = TemplateType.Kuendigung,
                        Title = "Kündigungsandrohung / Kündigung",
                        Content = "Kündigung: Kredit {Creditnumber}\n\nSehr geehrte/r {Name},\nda die Summe von {amount} (Fälligkeit {dueDate}) nicht eingegangen ist, wird der Kreditvertrag gekündigt.",
                        UpdatedAt = DateTime.UtcNow
                    }
                );
                context.SaveChanges();
            }

            if (!context.Customers.Any())
            {
                var today = DateTime.UtcNow.Date;
                context.Customers.AddRange(
                    new Customer
                    {
                        Name = "Max Mustermann",
                        PhoneNumber = "+491512345678",
                        CreditNumber = "CR-2026-1001",
                        Amount = 250.00m,
                        DueDate = today.AddDays(5),
                        CreatedAt = DateTime.UtcNow
                    },
                    new Customer
                    {
                        Name = "Erika Musterfrau",
                        PhoneNumber = "+491609876543",
                        CreditNumber = "CR-2026-1002",
                        Amount = 500.50m,
                        DueDate = today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow
                    },
                    new Customer
                    {
                        Name = "Johannes Schmidt",
                        PhoneNumber = "+491711122334",
                        CreditNumber = "CR-2026-1003",
                        Amount = 1200.00m,
                        DueDate = today.AddDays(-14),
                        CreatedAt = DateTime.UtcNow
                    },
                    new Customer
                    {
                        Name = "Anna Weber",
                        PhoneNumber = "+491520033445",
                        CreditNumber = "CR-2026-1004",
                        Amount = 75.25m,
                        DueDate = today.AddDays(10),
                        CreatedAt = DateTime.UtcNow
                    }
                );
                context.SaveChanges();
            }
        }
    }
}
