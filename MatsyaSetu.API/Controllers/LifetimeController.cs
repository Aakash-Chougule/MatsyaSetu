
using Microsoft.AspNetCore.Mvc;
using MatsyaSetu.Application.Services;

namespace MatsyaSetu.API.Controllers
{
    [ApiController]
    [Route("api/lifetime")]
    public class LifetimeController : ControllerBase
    {
        private readonly ITransientService _transient1;
        private readonly ITransientService _transient2;

        private readonly IScopedService _scoped1;
        private readonly IScopedService _scoped2;

        private readonly ISingletonService _singleton1;
        private readonly ISingletonService _singleton2;

        // Constructor Injection
        public LifetimeController(
            ITransientService transient1,
            ITransientService transient2,
            IScopedService scoped1,
            IScopedService scoped2,
            ISingletonService singleton1,
            ISingletonService singleton2)
        {
            _transient1 = transient1;
            _transient2 = transient2;

            _scoped1 = scoped1;
            _scoped2 = scoped2;

            _singleton1 = singleton1;
            _singleton2 = singleton2;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                Transient1 = _transient1.Id,
                Transient2 = _transient2.Id,

                Scoped1 = _scoped1.Id,
                Scoped2 = _scoped2.Id,

                Singleton1 = _singleton1.Id,
                Singleton2 = _singleton2.Id
            });
        }
    }
}
