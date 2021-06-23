using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Staks.Api.Infrastructure;
using Staks.Api.Models;
using Staks.Data.Entities;
using Staks.Data.Interfaces;
using Staks.Notifications.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Profile = Staks.Data.Entities.Profile;

namespace Staks.Api.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = StaksAuthSchemes.Musician)]
    [Produces("application/json")]
    [Route("bands/{staksName}/members")]
    public class BandMemberController : StaksControllerBase
    {
        private readonly IBusinessRepository _bandRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _performanceRepository;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public BandMemberController(
            IBusinessRepository bandRepository,
            IUserRepository userRepository,
            IEventRepository performanceRepository,
            INotificationService notificationService,
            IMapper mapper)
        {
            _bandRepository = bandRepository;
            _userRepository = userRepository;
            _performanceRepository = performanceRepository;
            _notificationService = notificationService;
            _mapper = mapper;
        }

        [HttpPost]
        [ProfileOwnership("staksName", new BandMemberRole[] { BandMemberRole.Owner, BandMemberRole.Admin })]
        public async Task<ActionResult<BandDetailModel>> AddBandMemberAsync(string staksName,
            BandMemberCreateModel member)
        {
            var band = await _bandRepository.GetByStaksNameAsync(staksName);
            if (band is null)
            {
                return NotFound();
            }

            var newMemberUser = await _userRepository.GetByStaksNameAsync(member.StaksName);
            if (newMemberUser is null)
            {
                return BadRequest("member", $"User {member.StaksName} not found");
            }

            if (band.Members.Any(m => m.UserId == newMemberUser.Id))
            {
                return Ok(_mapper.Map<BandDetailModel>(band, UserAuthId));
            }

            var isPerforming = await IsPerformingAsync(band.Profile);
            if (isPerforming)
            {
                return BadRequest("staksName", "Band cannot be edited while performance is in progress");
            }

            var memberEntity = new Member
            {
                User = newMemberUser,
                Role = BandMemberRole.Member,
                TranasctionPercentage = 0
            };

            band.Members.Add(memberEntity);
            await _bandRepository.SaveAsync();

            await _notificationService.SendAddedToBandAsync(newMemberUser, band);

            return Ok(_mapper.Map<BandDetailModel>(band, UserAuthId));
        }

        [HttpPut("{username}")]
        [ProfileOwnership("staksName", new BandMemberRole[] { BandMemberRole.Owner, BandMemberRole.Admin })]
        public async Task<ActionResult<BandDetailModel>> UpdateBandMemberAsync(string staksName, string username,
            BandMemberUpdateModel memberUpdate)
        {
            var band = await _bandRepository.GetByStaksNameAsync(staksName);
            if (band is null)
            {
                return NotFound();
            }

            var member = band.Members.FirstOrDefault(m =>
                m.User.Profile.StaksName.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (member is null)
            {
                return NotFound();
            }

            if (member.Role == BandMemberRole.Owner)
            {
                return BadRequest("username", "Owner cannot be edited");
            }

            var isPerforming = await IsPerformingAsync(band.Profile);
            if (isPerforming)
            {
                return BadRequest("staksName", "Band cannot be edited while performance is in progress");
            }

            if (memberUpdate.Role == BandMemberRole.Owner)
            {
                var currentUser = band.Members.Single(m => m.User.AuthId == UserAuthId);
                if (currentUser.Role != BandMemberRole.Owner)
                {
                    return BadRequest("memberUpdate", "Only the owner can transfer the ownership");
                }

                currentUser.Role = BandMemberRole.Admin;
            }

            member.Role = memberUpdate.Role;
            await _bandRepository.SaveAsync();
            return Ok(_mapper.Map<BandDetailModel>(band, UserAuthId));
        }

        [HttpDelete("{username}")]
        public async Task<ActionResult<BandDetailModel>> DeleteBandMemberAsync(string staksName, string username)
        {
            var band = await _bandRepository.GetByStaksNameAsync(staksName);
            if (band is null)
            {
                return NotFound();
            }

            var member = band.Members.FirstOrDefault(m =>
                m.User.Profile.StaksName.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (member is null)
            {
                return NotFound();
            }

            if (member.Role == BandMemberRole.Owner)
            {
                return BadRequest("username", "Owner cannot be removed");
            }

            if (!CanRemoveMember(band, member))
            {
                return BadRequest("You don't have permission to perform this action.");
            }

            var isPerforming = await IsPerformingAsync(band.Profile);
            if (isPerforming)
            {
                return BadRequest("staksName", "Band cannot be edited while performance is in progress");
            }

            band.Members = band.Members.Where(m => m.UserId != member.UserId).ToList();

            if (member.TranasctionPercentage > 0 && band.Members.Count > 0)
            {
                int split = (int)Math.Floor((double)member.TranasctionPercentage / band.Members.Count);
                band.Members
                    .ForEach(member => member.TranasctionPercentage += split);
                band.Members
                    .Where(x => x.Role == BandMemberRole.Owner)
                    .First()
                    .TranasctionPercentage += 100 - band.Members.Sum(x => x.TranasctionPercentage);
            }

            await _bandRepository.SaveAsync();

            await _notificationService.SendRemovedFromBandAsync(member, band);

            return Ok(_mapper.Map<BandDetailModel>(band));
        }

        private async Task<bool> IsPerformingAsync(Profile profile)
        {
            var performance = await _performanceRepository.GetActiveForProfileAsync(profile.Id);
            return performance != null;
        }

        private bool CanRemoveMember(Business band, Member targetMember)
        {
            var requesterMember = band.Members.FirstOrDefault(m => m.User.AuthId == UserAuthId);
            var role = requesterMember?.Role;

            return role == BandMemberRole.Owner || role == BandMemberRole.Admin ||
                   requesterMember?.UserId == targetMember.UserId;
        }
    }
}
