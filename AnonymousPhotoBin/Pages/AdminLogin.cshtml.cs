using AnonymousPhotoBin.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AnonymousPhotoBin.Pages
{
    public class AdminLoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminPasswordProvider _adminPasswordProvider;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AdminLoginModel(
            ApplicationDbContext context,
            IAdminPasswordProvider adminPasswordProvider,
            SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _adminPasswordProvider = adminPasswordProvider;
            _signInManager = signInManager;
        }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPost(string password) {
            if (_adminPasswordProvider.IsValid(password))
            {
                var user = await _context.Users.SingleAsync(u => u.Id == "81976842-8543-4dac-9729-dde8117b994f");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToPage("Index");
            }

            return new PageResult();
        }
    }
}