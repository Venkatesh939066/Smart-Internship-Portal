using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartInternshipPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartInternshipPortal.Models
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Run migrations automatically
            await context.Database.MigrateAsync();

            // 2. Seed Roles
            string[] roles = { "Admin", "Company", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 3. Seed Skills
            var defaultSkills = new List<string>
            {
                "C#", "ASP.NET Core", "Python", "JavaScript", "SQL",
                "Azure", "Spark", "Power BI", "React", "HTML & CSS",
                "Machine Learning", "UI/UX Design", "Project Management"
            };

            foreach (var skillName in defaultSkills)
            {
                if (!await context.Skills.AnyAsync(s => s.Name == skillName))
                {
                    context.Skills.Add(new Skill { Name = skillName });
                }
            }
            await context.SaveChangesAsync();

            // Fetch skills from database to get IDs
            var dbSkills = await context.Skills.ToDictionaryAsync(s => s.Name, s => s.Id);

            // 4. Seed Users
            // A. Seed Admin
            var adminEmail = "admin@portal.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Admin User",
                    Role = "Admin",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Replace the original public homepages with career portals for existing
            // demo accounts. Custom URLs entered later by a company are preserved.
            var legacyCompanyHomepages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["company@linkedin.com"] = "https://www.linkedin.com",
                ["company@google.com"] = "https://www.google.com",
                ["company@microsoft.com"] = "https://www.microsoft.com",
                ["company@meta.com"] = "https://www.meta.com",
                ["company@apple.com"] = "https://www.apple.com",
                ["company@netflix.com"] = "https://www.netflix.com"
            };

            // Helper to seed companies
            async Task<ApplicationUser> EnsureCompanySeeded(string email, string companyName, string description = "", string website = "", string industry = "")
            {
                var companyUser = await userManager.FindByEmailAsync(email);
                if (companyUser == null)
                {
                    companyUser = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = companyName,
                        Role = "Company",
                        CompanyDescription = description,
                        CompanyWebsite = website,
                        CompanyIndustry = industry,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(companyUser, "Password123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(companyUser, "Company");
                    }
                }
                else
                {
                    bool modified = false;
                    if (string.IsNullOrEmpty(companyUser.CompanyDescription) && !string.IsNullOrEmpty(description))
                    {
                        companyUser.CompanyDescription = description;
                        modified = true;
                    }
                    var hasLegacyHomepage = legacyCompanyHomepages.TryGetValue(email, out var legacyHomepage)
                        && string.Equals(
                            companyUser.CompanyWebsite?.TrimEnd('/'),
                            legacyHomepage.TrimEnd('/'),
                            StringComparison.OrdinalIgnoreCase);

                    if ((string.IsNullOrEmpty(companyUser.CompanyWebsite) || hasLegacyHomepage)
                        && !string.IsNullOrEmpty(website)
                        && !string.Equals(companyUser.CompanyWebsite, website, StringComparison.OrdinalIgnoreCase))
                    {
                        companyUser.CompanyWebsite = website;
                        modified = true;
                    }
                    if (string.IsNullOrEmpty(companyUser.CompanyIndustry) && !string.IsNullOrEmpty(industry))
                    {
                        companyUser.CompanyIndustry = industry;
                        modified = true;
                    }
                    if (modified)
                    {
                        await userManager.UpdateAsync(companyUser);
                    }
                }
                return companyUser;
            }

            // B. Seed Companies (Indian internship companies)
            var coTechCorp = await EnsureCompanySeeded("company@tatadigital.com", "Tata Digital",
                "Tata Digital builds secure, scalable products for India’s digital economy, focusing on payments, retail, and enterprise solutions.",
                "https://www.tatadigital.com", "Digital Services");
            var coInnovate = await EnsureCompanySeeded("company@infosys.com", "Infosys Labs",
                "Infosys Labs delivers AI-enabled enterprise solutions and cloud services for modern business transformation.",
                "https://www.infosys.com/careers", "Enterprise Technology");
            var coLinkedIn = await EnsureCompanySeeded("company@wipro.com", "Wipro Solutions",
                "Wipro builds intelligent automation systems and enterprise-grade applications for customers across India and the world.",
                "https://careers.wipro.com", "IT Services");
            var coGoogle = await EnsureCompanySeeded("company@reliance.com", "Reliance Digital",
                "Reliance Digital creates technology-led business solutions in retail, telecom, and digital commerce for Indian consumers.",
                "https://www.ril.com/careers", "Digital Commerce");
            var coMicrosoft = await EnsureCompanySeeded("company@flipkart.com", "Flipkart Labs",
                "Flipkart Labs works on large-scale e-commerce engineering, logistics intelligence, and customer experience innovations.",
                "https://www.flipkartcareers.com", "E-commerce Technology");
            var coMeta = await EnsureCompanySeeded("company@hcl.com", "HCL Technologies",
                "HCL Technologies delivers enterprise cloud, security and software engineering services to global and Indian businesses.",
                "https://www.hcltech.com/careers", "Technology Services");
            var coApple = await EnsureCompanySeeded("company@zoho.com", "Zoho",
                "Zoho builds cloud software for business productivity, automation, and customer relationship management.",
                "https://www.zoho.com/careers.html", "Cloud Software");
            var coNetflix = await EnsureCompanySeeded("company@paytm.com", "Paytm",
                "Paytm powers digital payments, commerce, and financial services for millions of users across India.",
                "https://paytm.com/careers", "Fintech");
            var coAmazon = await EnsureCompanySeeded("company@ola.com", "Ola Mobility",
                "Ola Mobility creates intelligent mobility services, electric vehicle platforms, and mobility-as-a-service solutions.",
                "https://www.olacabs.com/careers", "Mobility");
            var coTesla = await EnsureCompanySeeded("company@delhivery.com", "Delhivery",
                "Delhivery builds logistics automation and supply chain technology to support e-commerce across India.",
                "https://www.delhivery.com/careers", "Logistics Tech");
            var coSalesforce = await EnsureCompanySeeded("company@tcs.com", "TCS",
                "TCS offers enterprise solutions, consulting, and digital engineering services for customers across industries.",
                "https://www.tcs.com/careers", "IT Services");
            var coSpotify = await EnsureCompanySeeded("company@byjus.com", "BYJU'S",
                "BYJU'S builds personalized learning products and edtech platforms for students across India.",
                "https://byjus.com/careers", "EdTech");
            var coDeloitte = await EnsureCompanySeeded("company@zomato.com", "Zomato",
                "Zomato builds food delivery and restaurant discovery products with a focus on mobile-first user experiences.",
                "https://www.zomato.com/careers", "FoodTech");
            var coEY = await EnsureCompanySeeded("company@acko.com", "Acko",
                "Acko delivers digital-first insurance and fintech products using AI, analytics, and cloud-native services.",
                "https://www.acko.com/careers", "Insurtech");
            var coIBM = await EnsureCompanySeeded("company@olaelectric.com", "Ola Electric",
                "Ola Electric is building next-generation electric vehicles, charging networks, and sustainable mobility solutions.",
                "https://olaelectric.com/careers", "Electric Mobility");
            var coAccenture = await EnsureCompanySeeded("company@cred.com", "CRED",
                "CRED builds premium financial experiences, credit card management, and rewards systems for Indian users.",
                "https://www.cred.club/careers", "Fintech");

            var coSwiggy = await EnsureCompanySeeded("company@swiggy.com", "Swiggy",
                "Swiggy is India’s leading on-demand convenience platform, delivering food, groceries, and daily essentials.",
                "https://careers.swiggy.com", "On-Demand Delivery");
            var coRazorpay = await EnsureCompanySeeded("company@razorpay.com", "Razorpay",
                "Razorpay is the leading financial services platform powering online payments, payouts, and credit for businesses in India.",
                "https://razorpay.com/jobs", "Fintech");
            var coTechMahindra = await EnsureCompanySeeded("company@techmahindra.com", "Tech Mahindra",
                "Tech Mahindra offers innovative and customer-centric digital experiences, enabling enterprises to transition to digital systems.",
                "https://www.techmahindra.com/en-in/careers", "Consulting & IT");
            var coPhonePe = await EnsureCompanySeeded("company@phonepe.com", "PhonePe",
                "PhonePe is India's leading digital payments app, powering transactions, investments, insurance and shopping.",
                "https://www.phonepe.com/careers", "Fintech");

            // C. Seed Student
            var studentEmail = "student@portal.com";
            var studentUser = await userManager.FindByEmailAsync(studentEmail);
            if (studentUser == null)
            {
                studentUser = new ApplicationUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    FullName = "John Student",
                    Role = "Student",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(studentUser, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(studentUser, "Student");

                    // Seed Student Skills: C#, SQL, HTML & CSS
                    var studentSkills = new List<string> { "C#", "SQL", "HTML & CSS" };
                    foreach (var sName in studentSkills)
                    {
                        if (dbSkills.TryGetValue(sName, out int skillId))
                        {
                            context.UserSkills.Add(new UserSkill
                            {
                                UserId = studentUser.Id,
                                SkillId = skillId
                            });
                        }
                    }
                    await context.SaveChangesAsync();
                }
            }

            // 5. Seed Internships
            async Task AddOrUpdateInternship(string title, string description, string location, string companyId, List<string> requiredSkills, int offsetDays)
            {
                var internship = await context.Internships
                    .Include(i => i.InternshipSkills)
                    .FirstOrDefaultAsync(i => i.Title == title && i.CompanyId == companyId);

                var postedDate = DateTime.Now.AddDays(-offsetDays);
                var requiredSkillIds = requiredSkills
                    .Where(name => dbSkills.ContainsKey(name))
                    .Select(name => dbSkills[name])
                    .ToHashSet();

                if (internship == null)
                {
                    internship = new Internship
                    {
                        Title = title,
                        Description = description,
                        CompanyId = companyId,
                        Location = location,
                        PostedDate = postedDate
                    };
                    context.Internships.Add(internship);
                    await context.SaveChangesAsync();
                }
                else
                {
                    bool modified = false;
                    if (internship.Description != description)
                    {
                        internship.Description = description;
                        modified = true;
                    }
                    if (internship.Location != location)
                    {
                        internship.Location = location;
                        modified = true;
                    }
                    if (internship.PostedDate.Date != postedDate.Date)
                    {
                        internship.PostedDate = postedDate;
                        modified = true;
                    }

                    if (modified)
                    {
                        context.Internships.Update(internship);
                    }
                }

                var existingSkillIds = internship.InternshipSkills
                    .Select(isk => isk.SkillId)
                    .ToHashSet();

                var skillsToAdd = requiredSkillIds.Except(existingSkillIds).ToList();
                var skillsToRemove = internship.InternshipSkills
                    .Where(isk => !requiredSkillIds.Contains(isk.SkillId))
                    .ToList();

                foreach (var skillId in skillsToAdd)
                {
                    context.InternshipSkills.Add(new InternshipSkill
                    {
                        InternshipId = internship.Id,
                        SkillId = skillId
                    });
                }
                if (skillsToRemove.Any())
                {
                    context.InternshipSkills.RemoveRange(skillsToRemove);
                }

                await context.SaveChangesAsync();
            }

            // A. TechCorp (Tata Digital)
            await AddOrUpdateInternship(
                "Software Engineer Intern",
                "Join TechCorp as a Software Engineer Intern! You will work on building scalable web APIs, integrating relational databases, and writing unit tests. Great mentorship provided.",
                "Bengaluru, India",
                coTechCorp.Id,
                new List<string> { "C#", "ASP.NET Core", "SQL" },
                5
            );
            await AddOrUpdateInternship(
                "Data Scientist Intern",
                "Explore industrial machine learning pipelines and predict retail trends. You will work on feature extraction, construct deep learning frameworks, and run SQL queries.",
                "Bengaluru, India / Hybrid",
                coTechCorp.Id,
                new List<string> { "Python", "Machine Learning", "SQL" },
                3
            );
            await AddOrUpdateInternship(
                "Product Manager Intern",
                "Assist our product leads in mapping digital service roadmaps. Define features, coordinate technical requirements, and prototype screens using HTML & CSS.",
                "Mumbai, India / Hybrid",
                coTechCorp.Id,
                new List<string> { "Project Management", "HTML & CSS" },
                4
            );

            // B. InnovateSoft (Infosys Labs)
            await AddOrUpdateInternship(
                "Frontend Developer Intern",
                "InnovateSoft is looking for a creative Frontend Intern to design beautiful web user interfaces. You will code responsive features, work with React hooks, and optimize rendering speed.",
                "Mumbai, India / Hybrid",
                coInnovate.Id,
                new List<string> { "JavaScript", "React", "HTML & CSS" },
                3
            );
            await AddOrUpdateInternship(
                "UI/UX Designer Intern",
                "Infosys Labs is looking for a talented UI/UX design intern to construct high-fidelity application screens, develop wireframes, and design client mockups.",
                "Bengaluru, India / Hybrid",
                coInnovate.Id,
                new List<string> { "UI/UX Design", "HTML & CSS" },
                2
            );

            // C. LinkedIn (Wipro Solutions)
            await AddOrUpdateInternship(
                "Backend Engineer Intern",
                "Join the LinkedIn engineering team to build scalable microservices and tools that connect millions of professionals. You will write high-performance C# code and interface with SQL databases.",
                "Pune, India / Hybrid",
                coLinkedIn.Id,
                new List<string> { "C#", "ASP.NET Core", "SQL" },
                4
            );
            await AddOrUpdateInternship(
                "Cloud Solutions Intern",
                "Wipro Solutions is seeking a Cloud Intern to configure Azure resource groups, monitor server metrics, and automate deployment scripts.",
                "Pune, India / Remote",
                coLinkedIn.Id,
                new List<string> { "Azure", "C#" },
                3
            );

            // D. Google
            await AddOrUpdateInternship(
                "Software Engineering Intern",
                "As an intern at Google, you will collaborate on core algorithms, explore machine learning distributions, and develop automation scripts. Strong programming knowledge is required.",
                "Gurgaon, India / Remote",
                coGoogle.Id,
                new List<string> { "Python", "Machine Learning", "JavaScript" },
                2
            );

            // E. Microsoft
            await AddOrUpdateInternship(
                "Cloud Developer Intern",
                "Work with the Azure Engineering Group to build scalable cloud solutions and services. You will code in C#, design database schemas, and deploy containerized services.",
                "Bengaluru, India / Hybrid",
                coMicrosoft.Id,
                new List<string> { "C#", "ASP.NET Core", "SQL" },
                6
            );

            // F. Meta
            await AddOrUpdateInternship(
                "Full Stack Product Intern",
                "Help us design the future of social connection. You will work on product features, write clean client-side React code, and construct backend APIs.",
                "Menlo Park, CA / Office",
                coMeta.Id,
                new List<string> { "JavaScript", "React", "HTML & CSS" },
                1
            );

            // G. Apple
            await AddOrUpdateInternship(
                "UI/UX Design & Developer Intern",
                "At Apple, you will prototype beautiful, user-centric interfaces. You will bridge the gap between interface design and front-end development using HTML, CSS, and modern JavaScript.",
                "Cupertino, CA / Office",
                coApple.Id,
                new List<string> { "UI/UX Design", "HTML & CSS", "JavaScript" },
                7
            );

            // H. Netflix
            await AddOrUpdateInternship(
                "Data Engineering Intern",
                "Netflix is seeking a Data Analyst and Engineer Intern to optimize stream metadata pipelines, write complex SQL reports, and manage streaming metric dashboards.",
                "Los Gatos, CA / Remote",
                coNetflix.Id,
                new List<string> { "Python", "SQL", "Project Management" },
                8
            );

            // I. Amazon
            await AddOrUpdateInternship(
                "AWS Cloud Support Intern",
                "Amazon Web Services seeks an intern to help support cloud customers, troubleshoot distributed systems, and learn AWS architecture.",
                "Seattle, WA / Remote",
                coAmazon.Id,
                new List<string> { "Python", "Linux", "Cloud Computing" },
                4
            );

            // J. Tesla
            await AddOrUpdateInternship(
                "Autonomy Software Intern",
                "Tesla is hiring interns to work on real-time autonomy systems for self-driving cars. You will write simulations, analyze sensor data, and contribute to vehicle intelligence.",
                "Palo Alto, CA / Office",
                coTesla.Id,
                new List<string> { "Python", "C++", "Machine Learning" },
                3
            );

            // K. Salesforce
            await AddOrUpdateInternship(
                "Salesforce Platform Intern",
                "Join Salesforce to help build CRM platform features, automation workflows, and customer collaboration tools for modern enterprise teams.",
                "San Francisco, CA / Hybrid",
                coSalesforce.Id,
                new List<string> { "JavaScript", "Apex", "Cloud CRM" },
                5
            );

            // L. Spotify
            await AddOrUpdateInternship(
                "Data Science Intern",
                "Spotify is looking for interns to work on music recommendation models, analytics pipelines, and user personalization research.",
                "New York, NY / Remote",
                coSpotify.Id,
                new List<string> { "Python", "Machine Learning", "Data Analysis" },
                6
            );

            // M. Deloitte
            await AddOrUpdateInternship(
                "Business Technology Analyst Intern",
                "Deloitte seeks interns to support business transformation projects, analyze technology solutions, and help clients adopt digital innovation.",
                "Multiple Locations / Hybrid",
                coDeloitte.Id,
                new List<string> { "C#", "SQL", "Project Management" },
                5
            );

            // N. EY
            await AddOrUpdateInternship(
                "Technology Risk Intern",
                "EY is seeking interns to assess technology risks, support digital assurance processes, and help clients improve controls.",
                "Multiple Locations / Hybrid",
                coEY.Id,
                new List<string> { "Python", "Data Analysis", "Project Management" },
                4
            );

            // O. IBM
            await AddOrUpdateInternship(
                "AI Research Intern",
                "IBM invites interns to work on AI research projects, build machine learning models, and deploy intelligent applications.",
                "Armonk, NY / Remote",
                coIBM.Id,
                new List<string> { "Python", "Machine Learning", "Data Analysis" },
                3
            );

            // P. Accenture
            await AddOrUpdateInternship(
                "Consulting Technology Intern",
                "Accenture is looking for interns to help deliver digital transformation solutions and support enterprise technology projects.",
                "Multiple Locations / Hybrid",
                coAccenture.Id,
                new List<string> { "JavaScript", "Cloud Computing", "Project Management" },
                2
            );

            // Q. Swiggy
            await AddOrUpdateInternship(
                "Data Analyst Intern",
                "Swiggy is looking for a Data Analyst Intern to join our logistics intelligence team. You will write SQL queries, analyze order routing efficiency, and build real-time dashboards.",
                "Bengaluru, India / Hybrid",
                coSwiggy.Id,
                new List<string> { "SQL", "Python", "Power BI" },
                2
            );

            // R. Razorpay
            await AddOrUpdateInternship(
                "Backend Engineering Intern (Fintech)",
                "Join Razorpay's payments platform team. You will build secure APIs, work with database transactions, and help optimize checkout flows.",
                "Bengaluru, India / Office",
                coRazorpay.Id,
                new List<string> { "C#", "SQL", "JavaScript" },
                4
            );

            // S. Tech Mahindra
            await AddOrUpdateInternship(
                "Systems Engineer Intern",
                "Tech Mahindra is hiring interns for our cloud infrastructure team. You will assist in automating cloud deployments, configuring Azure environments, and troubleshooting servers.",
                "Pune, India / Office",
                coTechMahindra.Id,
                new List<string> { "Azure", "Python", "Project Management" },
                5
            );

            // T. PhonePe
            await AddOrUpdateInternship(
                "Software Engineer Intern (Android/iOS)",
                "Work on India's largest payment interface. You will prototype user interfaces, optimize mobile application speed, and write unit tests.",
                "Bengaluru, India / Office",
                coPhonePe.Id,
                new List<string> { "JavaScript", "React", "UI/UX Design" },
                3
            );
        }
    }
}
