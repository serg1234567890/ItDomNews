using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Tsg.Api.Identity;
using Tsg.EFStore;
using Tsg.EFStore.Filters;
using Tsg.Entity.Faq;
using Tsg.Entity.Firebase;
using Tsg.Entity.ManyToMany;
using Tsg.Entity.Ping;
using Tsg.Entity.Tidings;
using Tsg.Entity.Unused;


namespace Tsg.Api.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : BaseUtilsController
    {
        private readonly CustomerStoreService customerStore;
        private readonly EmployeeStoreService employeeStore;
        private readonly TsgUserManager userManager;
        private readonly UnusedStoreService unusedStoreService;
        private readonly TidingStoreService tidingStoreService;
        private readonly TidingTargetStoreService tidingTargetStoreService;
        private readonly CustomersToPlacementsStoreService customersToPlacementsStore;
        private readonly PlacementStoreService placementStore;
        private readonly FirebaseStoreService firebaseStoreService;

        public NewsController(UnusedStoreService unusedStoreService_,
            EmployeeStoreService employeeStore_,
            TsgUserManager userManager_,
            TidingStoreService tidingStoreService_,
            TidingTargetStoreService tidingTargetStoreService_,
            CustomerStoreService customerStore_,
            CustomersToPlacementsStoreService customersToPlacementsStore_,
            PlacementStoreService placementStore_,
            FirebaseStoreService firebaseStoreService_,
            SystemStoreService systemStoreService_) : base(employeeStore_, userManager_, customerStore_, systemStoreService_)
        {
            this.unusedStoreService = unusedStoreService_;
            this.employeeStore = employeeStore_;
            this.userManager = userManager_;
            this.customerStore = customerStore_;
            this.tidingStoreService = tidingStoreService_;
            this.tidingTargetStoreService = tidingTargetStoreService_;
            this.customersToPlacementsStore = customersToPlacementsStore_;
            this.placementStore = placementStore_;
            this.firebaseStoreService = firebaseStoreService_;
        }
        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> Get(long? tidingId, bool? includetarget = false)
        {
            try
            {
                Guid? userId = Authorize();
                if (userId == null)
                {
                    return StatusCode(401, "Bad token");
                }
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return StatusCode(500, "User not found");
                }
                if (!tidingId.HasValue)
                {
                    return StatusCode(500, "Tiding ID not defined");
                }
                long companyId = GetCompanyIdForAllRoles().Result.Value;
                var companynews = await tidingStoreService.GetAllAsync(companyId);
                if (!includetarget.HasValue) includetarget = false;

                var tiding = await tidingStoreService.GetById(tidingId.Value);
                var tidingviews = new TidingView(tiding, companynews.Count, includetarget.Value);

                return Ok(tidingviews);
            }
            catch (Exception e)
            {
                Log.Error("\nMessageError: {0} \n StackTrace: {1}", e.Message, e.StackTrace);
                return StatusCode(500);
            }
        }
        [HttpGet]
        [Route("page")]
        public async Task<IActionResult> Page(long? page = 1, bool? includetarget = false, bool? includepager = false)
        {
            try
            {
                Guid? userId = Authorize();
                if (userId == null)
                {
                    return StatusCode(401, "Bad token");
                }
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return StatusCode(500, "User not found");
                }
                var customersPlacements = await placementStore.GetCustomersPlacements(user);

                List<long?> targets = new List<long?>();
                foreach (var pl in customersPlacements)
                {
                    if (pl.Object == null) continue;

                    long? placementCompanyId = pl.Object.CompanyId;
                    if (!placementCompanyId.HasValue) continue;

                    var nextgroup = await tidingTargetStoreService.GetCustomerTarget(placementCompanyId, pl.ObjectId, pl.Entrance, pl.Id);
                    if (nextgroup.Count == 0) continue;
                    targets.AddRange(nextgroup);

                }
                List<Tiding> customernews = new List<Tiding>();
                foreach (long? tidingid in targets.Distinct())
                {
                    var tiding = await tidingStoreService.GetById(tidingid.Value);
                    if (tiding == null) continue;
                    customernews.Add(tiding);
                }

                //var companynews = await tidingStoreService.GetAllAsync(companyId.Value);
                int companynewstotal = customernews.Count;

                int pagesize = 20;
                if (!page.HasValue) page = 1;
                if (page.Value == 0) page = 1;
                
                int startindexforpage = (int)(page.Value - 1) * pagesize;
                if (companynewstotal <= pagesize) startindexforpage = 0;
                else if (startindexforpage >= companynewstotal) startindexforpage = companynewstotal - pagesize;
                
                if (!includetarget.HasValue) includetarget = false;

                var nextpage = customernews.Select(_ => new TidingView(_, companynewstotal, includetarget.Value)).OrderByDescending(_ => _.Id).Skip(startindexforpage).Take(pagesize);
                
                if (includepager.HasValue && includepager.Value)
                {
                    var pager = new Common.Helpers.Pager(companynewstotal, (int)page.Value, pagesize);
                    var paging = new
                    {
                        Collection = nextpage,
                        Pager = pager
                    };
                    return Ok(paging);
                }
                else
                {
                    return Ok(nextpage);
                }
            }
            catch (Exception e)
            {
                Log.Error("\nMessageError: {0} \n StackTrace: {1}", e.Message, e.StackTrace);
                return StatusCode(500);
            }
        }
        [HttpGet]
        [Route("dispatcherpage")]
        public async Task<IActionResult> Dispatcherpage(long? companyId, long? page = 1, bool? includetarget = false, bool? includepager = false)
        {
            try
            {
                Guid? userId = Authorize();
                if (userId == null)
                {
                    return StatusCode(401, "Bad token");
                }
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return StatusCode(500, "User not found");
                }
                if (!companyId.HasValue)
                {
                    return StatusCode(500, "Company not found");
                }
                var news = await tidingStoreService.GetAllAsync(companyId.Value);

                //var companynews = await tidingStoreService.GetAllAsync(companyId.Value);
                int total = news.Count;

                int pagesize = 10;
                if (!page.HasValue) page = 1;
                if (page.Value == 0) page = 1;

                int startindexforpage = (int)(page.Value - 1) * pagesize;
                if (total <= pagesize) startindexforpage = 0;
                else if (startindexforpage >= total) startindexforpage = total - pagesize;

                if (!includetarget.HasValue) includetarget = false;

                var newsviews = news.Select(_ => new TidingView(_, news.Count, true)).OrderByDescending(_ => _.Id).Skip(startindexforpage).Take(pagesize);

                if (includepager.HasValue && includepager.Value)
                {
                    var pager = new Common.Helpers.Pager(total, (int)page.Value, pagesize);
                    var paging = new
                    {
                        Collection = newsviews,
                        Pager = pager
                    };
                    return Ok(paging);
                }
                else
                {
                    return Ok(newsviews);
                }
            }
            catch (Exception e)
            {
                Log.Error("\nMessageError: {0} \n StackTrace: {1}", e.Message, e.StackTrace);
                return StatusCode(500);
            }
        }
        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> List(long? companyId)
        {
            try
            {
                Guid? userId = Authorize();
                if (userId == null)
                {
                    return StatusCode(401, "Bad token");
                }
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return StatusCode(500, "User not found");
                }
                if (!companyId.HasValue)
                {
                    return StatusCode(500, "Company not found");
                }

                var news = await tidingStoreService.GetAllAsync(companyId.Value);
                var newsviews = news.Select(_ => new TidingView(_, news.Count, true)).OrderByDescending(_ => _.Id);

                return Ok(newsviews);
            }
            catch (Exception e)
            {
                Log.Error("\nMessageError: {0} \n StackTrace: {1}", e.Message, e.StackTrace);
                return StatusCode(500);
            }
        }
        [HttpGet]
        [Route("ping")]
        public async Task<ActionResult<PingModel>> Ping()
        {
            try
            {
                Guid? userId = Authorize();
                if (userId == null)
                {
                    return StatusCode(401, "Bad token");
                }
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return StatusCode(500, "User not found");
                }
                long companyId = GetCompanyIdForAllRoles().Result.Value;

                PingModel ping = new PingModel();
                ping.IsAdmin = await userManager.IsInRoleAsync(user, "admin");
                //if (!ping.IsAdmin.Value) ping.IsAdmin = await userManager.IsInRoleAsync(user, "dispatcher");
                //if (!ping.IsAdmin.Value) ping.IsAdmin = await userManager.IsInRoleAsync(user, "manager");
                ping.CompanyId = companyId;

                return Ok(ping);
            }
            catch (Exception e)
            {
                Log.Error("\nMessageError: {0} \n StackTrace: {1}", e.Message, e.StackTrace);
                return StatusCode(500);
            }
        }
        [HttpPost]
        [Route("customerlist")]
        public async Task<ActionResult> Customerlist()
        {
            try
            {
                Guid? userId = Authorize();
                if (userId == null)
                {
                    return StatusCode(401, "Bad token");
                }
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return StatusCode(500, "User not found");
                }
                Entity.Customers.CustomerEntity customer = await customerStore.GetByUserIdAsync(user.Id);
                if (customer == null)
                {
                    return StatusCode(500, "Customer not found");
                }

                //var customersToPlacements = customersToPlacementsStore.GetPlacementsList(customer.Id);

                var customersPlacements = await placementStore.GetCustomersPlacements(user);

                List<long?> targets = new List<long?>();
                foreach (var pl in customersPlacements)
                {
                    if (pl.Object == null) continue;

                    long? companyId = pl.Object.CompanyId;
                    if (!companyId.HasValue) continue;

                    var nextgroup = await tidingTargetStoreService.GetCustomerTarget(companyId, pl.ObjectId, pl.Entrance, pl.Id);
                    if (nextgroup.Count == 0) continue;
                    targets.AddRange(nextgroup);

                }
                List<Tiding> news = new List<Tiding>();
                foreach (long? tidingid in targets.Distinct())
                {
                    var tiding = await tidingStoreService.GetById(tidingid.Value);
                    if (tiding == null) continue;
                    news.Add(tiding);
                }
                var newsviews = news.Select(_ => new TidingView(_, news.Count, true)).OrderByDescending(_ => _.Id);

                return Ok(newsviews);
            }
            catch (Exception e)
            {
                Log.Error("\nMessageError: {0} \n StackTrace: {1}", e.Message, e.StackTrace);
                return StatusCode(500);
            }
        }
        [HttpPost]
        [Route("update")]
        public async Task<ActionResult> update(TidingView model)
        {
            try
            {
                Guid? userId = Authorize();
                if (userId == null)
                {
                    return StatusCode(401, "Bad token");
                }
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return StatusCode(500, "User not found");
                }
                if (model == null)
                {
                    return BadRequest("Invalid model");
                }
                if (!model.Id.HasValue)
                {
                    return BadRequest("Invalid question");
                }
                if (!model.CompanyId.HasValue)
                {
                    return BadRequest("Invalid company ID");
                }

                var tiding = new Tiding();
                if (model.Id.Value != 0)
                {
                    tiding = await tidingStoreService.GetById(model.Id.Value);
                    tiding.Name = model.Name;
                    tiding.Subject = model.Subject;
                    tiding.CompanyId = model.CompanyId;
                }
                else
                {
                    tiding.CreateDate = DateTime.UtcNow;
                    tiding.Name = model.Name;
                    tiding.Subject = model.Subject;
                    tiding.CompanyId = model.CompanyId;
                    tidingStoreService.Insert(tiding);
                }
                tidingStoreService.Save();

                var targets = await tidingTargetStoreService.GetByTidingId(tiding.Id.Value);
                if (targets.Count > 0)
                {
                    targets.ForEach(a => a.DeleteDate = DateTime.UtcNow);
                    tidingTargetStoreService.Save();
                }

                List<TidingPush> alltargets = new List<TidingPush>();
                foreach (var target in model.tidingtargets)
                {
                    var entity = new TidingPush();
                    entity.CreateDate = DateTime.UtcNow;
                    if (target.CompanyId != null) entity.CompanyId = target.CompanyId;
                    entity.CreateDate = DateTime.UtcNow;
                    if (target.ObjectId != null) entity.ObjectId = target.ObjectId;
                    if (target.PlacementId != null) entity.PlacementId = target.PlacementId;
                    if (target.Podezd != null) entity.Podezd = target.Podezd;
                    if (target.TidingId != null) entity.TidingId = tiding.Id;
                    entity.Title = target.Title;

                    tidingTargetStoreService.Insert(entity);

                    entity.Name = tiding.Name;
                    entity.Subject = tiding.Subject;
                    entity.StartDate = tiding.StartDate;
                    alltargets.Add(entity);
                }
                tidingTargetStoreService.Save();

                foreach (var target in alltargets)
                {
                    if (!target.Podezd.HasValue && !target.PlacementId.HasValue)
                    {
                        var inobject = await placementStore.InObject(target.CompanyId.Value, target.ObjectId.Value);
                        if (inobject.Count > 0) target.AddPlacements(inobject);
                    }
                    else if (target.Podezd.HasValue && !target.PlacementId.HasValue)
                    {
                        var inentrance = await placementStore.InEntrance(target.CompanyId.Value, target.ObjectId.Value, (int)target.Podezd.Value);
                        if (inentrance.Count > 0) target.AddPlacements(inentrance);
                    }
                    else if (!target.Podezd.HasValue && target.PlacementId.HasValue)
                    {
                        var inplacement = await placementStore.InPlacement(target.PlacementId.Value);
                        if (inplacement.Count > 0) target.AddPlacements(inplacement);
                    }
                }

                foreach (var target in alltargets)
                {
                    foreach (var placementId in target.GetPlacements())
                    {
                        var customerToPlacementEntity = customersToPlacementsStore.GetByPlacementId(placementId.Value);
                        if (customerToPlacementEntity == null) continue;
                        var customer = await customerStore.GetById(customerToPlacementEntity.CustomerId);
                        if (customer == null) continue;
                        List<FirebaseEntity> tokens = await firebaseStoreService.GetTokens(customer.User.Id);

                        foreach (var entity in tokens)
                        {
                            if (entity.Token == null) continue;

                            string title = target.Name;
                            string body = Tsg.Api.Helpers.Tool.ConvertFromUTF16(target.Subject);

                            var result = Helpers.Http.PushNews(title, body, entity.Token, target.TidingId.Value);
                        }
                    }
                }
                var news = await tidingStoreService.GetAllAsync(model.CompanyId.Value);
                var newsviews = news.Select(_ => new TidingView(_, news.Count, true)).OrderByDescending(_ => _.Id);

                return Ok(newsviews);
            }
            catch (Exception e)
            {
                Log.Error("\nMessageError: {0} \n StackTrace: {1}", e.Message, e.StackTrace);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [Route("remove")]
        public async Task<ActionResult> remove(TidingView model)
        {
            try
            {
                Guid? userId = Authorize();
                if (userId == null)
                {
                    return StatusCode(401, "Bad token");
                }
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return StatusCode(500, "User not found");
                }
                if (model == null)
                {
                    return BadRequest("Invalid model");
                }
                if (!model.Id.HasValue)
                {
                    return BadRequest("Invalid question");
                }
                if (!model.CompanyId.HasValue)
                {
                    return BadRequest("Invalid company ID");
                }
                if (model.Id.Value > 0)
                {
                    var targets = await tidingTargetStoreService.GetByTidingId(model.Id.Value);
                    if (targets.Count > 0)
                    {
                        targets.ForEach(a => a.DeleteDate = DateTime.UtcNow);
                        tidingTargetStoreService.Save();
                    }

                    var tiding = await tidingStoreService.GetById(model.Id.Value);
                    tiding.DeleteDate = DateTime.UtcNow;
                    tidingStoreService.Save();
                }

                var news = await tidingStoreService.GetAllAsync(model.CompanyId.Value);
                var newsviews = news.Select(_ => new TidingView(_, news.Count, true)).OrderByDescending(_ => _.Id);

                return Ok(newsviews);
            }
            catch (Exception e)
            {
                Log.Error("\nMessageError: {0} \n StackTrace: {1}", e.Message, e.StackTrace);
                return StatusCode(500);
            }
        }
    }
}
