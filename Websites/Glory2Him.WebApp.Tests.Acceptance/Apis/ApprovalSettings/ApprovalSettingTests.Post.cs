// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalSettings;
using RESTFulSense.Exceptions;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalSettings
{
    public partial class ApprovalSettingApiTests
    {
        [Fact]
        public async Task ShouldPostApprovalSettingAsync()
        {
            // given
            ApprovalSetting randomApprovalSetting = CreateRandomApprovalSetting();
            ApprovalSetting inputApprovalSetting = randomApprovalSetting;
            ApprovalSetting expectedApprovalSetting = inputApprovalSetting;

            try
            {
                // when
                await this.apiBroker.PostApprovalSettingAsync(inputApprovalSetting);

                ApprovalSetting actualApprovalSetting =
                    await this.apiBroker.GetApprovalSettingByIdAsync(inputApprovalSetting.Id);

                // then
                actualApprovalSetting.Should().BeEquivalentTo(expectedApprovalSetting, options => options
                    .Excluding(property => property.CreatedBy)
                    .Excluding(property => property.CreatedWhen)
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen));
            }
            finally
            {
                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(inputApprovalSetting.Id);
            }
        }

        /// <summary>
        /// The pairs §8.6.2 refuses: either threshold off <c>ConfidenceScore</c>'s own
        /// 0.00–10.00 scale (§13.5), or an approval threshold below the rejection one.
        /// </summary>
        public static TheoryData<decimal, decimal> InvalidConfidenceThresholdPairs() =>
            new()
            {
                { -1.00m, 7.50m },
                { 11.00m, 7.50m },
                { 2.50m, 11.00m },
                { 7.50m, 2.50m },
            };

        /// <summary>
        /// The threshold rules, asserted from the outside.
        ///
        /// <para>Worth reaching over HTTP even though the service unit tests cover the rules
        /// themselves: this is the vector the finding names — a caller that is not the admin
        /// page, whose number inputs were the only thing enforcing the range before. The 400
        /// arrives from the service rather than from
        /// <c>CK_ApprovalSetting_AIThresholdOrder</c> and its two siblings, which sit behind it
        /// as the defence in depth (§14.6 rule 2); the scope constraints in
        /// <c>ApprovalSettingTests.Security</c> are the reverse case, reachable only in the
        /// database.</para>
        ///
        /// <para>Nothing is written on any of these rows, so the teardown is defensive — the
        /// same shape the scope refusals use, and the reason is the same: a rule that stopped
        /// refusing would leave a row behind, and the suite must not carry it into the next
        /// test.</para>
        /// </summary>
        [Theory]
        [MemberData(nameof(InvalidConfidenceThresholdPairs))]
        public async Task ShouldReturnBadRequestOnPostIfTheAIConfidenceThresholdsAreInvalidAsync(
            decimal invalidRejectionThreshold,
            decimal invalidApprovalThreshold)
        {
            // given
            ApprovalSetting invalidApprovalSetting = CreateRandomApprovalSetting();
            invalidApprovalSetting.AIApprovalConfidenceRejectionThreshold = invalidRejectionThreshold;
            invalidApprovalSetting.AIApprovalConfidenceApprovalThreshold = invalidApprovalThreshold;

            try
            {
                // when
                var postTask =
                    this.apiBroker.PostApprovalSettingAsync(invalidApprovalSetting).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => postTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    invalidApprovalSetting.Id);
            }
        }
    }
}
