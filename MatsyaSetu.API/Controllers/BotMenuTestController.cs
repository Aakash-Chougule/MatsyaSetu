using MatsyaSetu.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MatsyaSetu.API.Controllers
{
    [ApiController]
    [Route("api/dev/bot-menu")]
    public class BotMenuTestController : ControllerBase
    {
        private readonly IBotMenuRepository _botMenuRepository;

        public BotMenuTestController(
            IBotMenuRepository botMenuRepository)
        {
            _botMenuRepository = botMenuRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuItems(
            CancellationToken cancellationToken)
        {
            var menuItems =
                await _botMenuRepository.GetActiveMenuItemsAsync(
                    cancellationToken);

            return Ok(menuItems);
        }
    }
}