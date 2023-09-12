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
        private readonly SignInManager<IdentityUser> _signInManager;

        public AdminLoginModel(
            ApplicationDbContext context,
            SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _signInManager = signInManager;
        }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPost(string password) {
            var user = await _context.Users.SingleAsync(u => u.Id == "81976842-8543-4dac-9729-dde8117b994f");
            if (password == "abcd")
                await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToPage("Index");
        }
    }
}