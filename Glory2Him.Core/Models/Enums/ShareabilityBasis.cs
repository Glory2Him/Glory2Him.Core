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

namespace Glory2Him.Core.Models.Enums
{
    /// <summary>
    /// Represents the basis on which a content item is permitted to be shared.
    /// </summary>
    /// <remarks>
    /// TWO QUESTIONS, ONE MEMBER EACH. Who wrote it (the contributor, or somebody else) and
    /// what the site may do with it (redistribute it freely, or share it under a permission).
    /// The four live members are that pair crossed, because the two answers together decide
    /// the licence and neither one alone does: "it's my own" says nothing about whether the
    /// contributor is releasing it, and "public domain" says nothing about who wrote it.
    ///
    /// APPEND-ONLY, and the members are persisted BY NAME — the storage broker converts this
    /// property with <c>HasConversion&lt;string&gt;()</c>, so a stored row reads "PublicDomain"
    /// rather than 2. The numbers are still a contract: the host registers no
    /// JsonStringEnumConverter, so they are what crosses the wire to the React client, and the
    /// TypeScript mirror is numbered to match.
    /// </remarks>
    public enum ShareabilityBasis
    {
        /// <summary>
        /// RETIRED — the contributor owns the content outright, with no statement of what the
        /// site may do with it. Superseded by <see cref="OwnedPermissionGranted"/> and
        /// <see cref="OwnedPublicDomain"/>, which split it by the licence the contributor is
        /// actually granting.
        /// </summary>
        /// <remarks>
        /// STILL VALID, and deliberately so. Every row contributed before the split carries it,
        /// and reclassifying those rows would put a licence claim on somebody's work that they
        /// never made — so they keep the member they were filed under and the reading surfaces
        /// name it plainly. It is not offered to a contributor: the picker lists the four
        /// members below and this one appears only on a row that already holds it.
        ///
        /// It is also the column's stored default, which is the right default precisely because
        /// it is the weakest claim: a row that somehow arrives without the column asserts the
        /// least.
        /// </remarks>
        Owned = 0,

        /// <summary>
        /// Somebody else owns the content and has granted the contributor permission to share
        /// it. <see cref="Models.Foundations.ContentItems.ContentItem.SharePermission"/>
        /// optionally records the detail of that permission.
        /// </summary>
        PermissionGranted = 1,

        /// <summary>
        /// Somebody else wrote the content, and it is in the public domain — no owner
        /// permission is needed to share it.
        /// </summary>
        PublicDomain = 2,

        /// <summary>
        /// The contributor owns the content, and grants permission for it to be shared here.
        /// The narrower of the two ownership members: the contributor keeps their rights and
        /// licenses this use, rather than giving the work away.
        /// </summary>
        OwnedPermissionGranted = 3,

        /// <summary>
        /// The contributor owns the content and is releasing it into the public domain, so it
        /// may be shared onward freely.
        /// </summary>
        OwnedPublicDomain = 4
    }
}
