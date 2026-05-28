// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.Attachments;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using LeVent.Clients;

namespace Glory2Him.Core.Brokers.Events
{
    public partial class EventBroker : IEventBroker
    {
        public EventBroker()
        {
            this.ApprovalCommentEvents = new LeVentClient<EventEnvelope<ApprovalComment>>();
            this.ApprovalEvents = new LeVentClient<EventEnvelope<Approval>>();
            this.ApprovalReviewEvents = new LeVentClient<EventEnvelope<ApprovalReview>>();
            this.ApprovalSettingEvents = new LeVentClient<EventEnvelope<ApprovalSetting>>();
            this.ApprovalSettingRoleEvents = new LeVentClient<EventEnvelope<ApprovalSettingRole>>();
            this.AttachmentEvents = new LeVentClient<EventEnvelope<Attachment>>();
            this.BibleReferenceEvents = new LeVentClient<EventEnvelope<BibleReference>>();
            this.CommentEvents = new LeVentClient<EventEnvelope<Comment>>();
            this.ContentItemAssociationEvents = new LeVentClient<EventEnvelope<ContentItemAssociation>>();
            this.ContentItemEvents = new LeVentClient<EventEnvelope<ContentItem>>();
            this.ContentItemSettingEvents = new LeVentClient<EventEnvelope<ContentItemSetting>>();
            this.ContentTypeEvents = new LeVentClient<EventEnvelope<ContentType>>();
            this.LinkEvents = new LeVentClient<EventEnvelope<Link>>();
            this.ReactionEvents = new LeVentClient<EventEnvelope<Reaction>>();
            this.TagEvents = new LeVentClient<EventEnvelope<Tag>>();
        }
    }
}
