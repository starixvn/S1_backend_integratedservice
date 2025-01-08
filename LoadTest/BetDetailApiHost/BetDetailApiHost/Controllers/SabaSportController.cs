using Microsoft.AspNetCore.Mvc;
using BetDetailApiHost.Helper;
using BetDetailApiHost.Models.ConfirmbetParlay;
using BetDetailApiHost.Models.PlaceBetParlay;
using BetDetailApiHost.Models.PlaceBet;
using BetDetailApiHost.Models.ConfirmBet;
using BetDetailApiHost.Models.Settle;
using BetDetailApiHost.Models.GetBetDetail;
using BetDetailApiHost.Repository;

namespace BetDetailApiHost.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SabaSportController(IMemoryDb memoryDb, IBetDetailStorage betDetailStorage) : ControllerBase
    {
        [HttpPost("BetDetail")]
        public async Task<IActionResult> AddBetDetail(List<BetDetail> betDetails)
        {
            Console.WriteLine($"Storage Size: {betDetailStorage.Count()}");
            await betDetailStorage.TryAddBatchBetDetailAsync(betDetails);

            return Ok();
        }

        [HttpPost("GetTickets")]
        public IActionResult GetTickets()
        {
            return Ok(betDetailStorage.GetBetDetailsListAsync());
        }

        [HttpPost("PlaceBetParlay")]
        public async Task<IActionResult> PlaceBetParlay(PlaceBetParlayRequest source)
        {
            await memoryDb.SetBetDetailKeyAsync(source);
            Console.WriteLine("PlaceBetParlay");

            return Ok();
        }

        [HttpPost("PlaceBet")]
        public async Task<IActionResult> PlaceBet(PlaceBetRequest source)
        {
            await memoryDb.SetBetDetailKeyAsync(source);
            Console.WriteLine("PlaceBet");

            return Ok();
        }

        [HttpPost("ConfirmBetParlay")]
        public async Task<IActionResult> ConfirmBetParlay(ConfirmBetParlayRequest source)
        {
            await memoryDb.SetBetDetailKeyAsync(source);
            Console.WriteLine("ConfirmBetParlay");

            return Ok();
        }

        [HttpPost("ConfirmBet")]
        public async Task<IActionResult> ConfirmBet(ConfirmBetRequest source)
        {
            await memoryDb.SetBetDetailKeyAsync(source);
            Console.WriteLine("ConfirmBet");

            return Ok();
        }

        [HttpPost("Settle")]
        public async Task<IActionResult> Settle(SettleRequest source)
        {
            await memoryDb.SetBetDetailKeyAsync(source);
            Console.WriteLine("Settle");

            return Ok();
        }
    }
}