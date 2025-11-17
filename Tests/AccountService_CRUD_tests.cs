//using AccountService.Business;
//using AccountService.Data;
//using AccountService.Models;
//using Microsoft.EntityFrameworkCore;
//using Moq;
//using System;

//namespace Tests
//{
//    public class AccountService_CRUD_tests
//    {
//        public class ProfileServiceTests
//        {
//            [Fact]
//            public void SearchProfiles_Returns_Matching_Users()
//            {
//                // Arrange
//                var options = new DbContextOptionsBuilder<AccountRepository>()
//                    .UseInMemoryDatabase("TestDb")
//                    .Options;

//                using var context = new AppDbContext(options);

//                context.Profile.Add(new Profile { Username = "aleksandar" });
//                context.Profile.Add(new Profile { Username = "alex123" });
//                context.Profile.Add(new Profile { Username = "john" });
//                context.SaveChanges();

//                var service = new ProfileService(context);

//                // Act
//                var result = service.SearchProfiles("alex");

//                // Assert
//                Assert.Equal(2, result.Count);
//            }
//        }
//    }
//}