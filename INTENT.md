# INTENT.md

## System Overview

This system provides a CRM-like content platform that enables users to create, manage, review, and publish structured content.

The application consists of:

* A secured API
* A web-based frontend

All functionality is centred around the creation, governance, and publication of content through structured workflows.

---

## Core Purpose

The system exists to:

* Capture meaningful content
* Enable collaboration and discussion
* Enforce governance through approvals
* Control visibility and interaction through configurable policies

---

## Domain Model Overview

The system is built around **Content Items**, which are versioned, governed entities that can be enriched with related features such as tags, reactions, links, comments, and attachments.

All significant actions are subject to approval workflows and policy rules.

---

## ContentItem

Represents the core unit of content.

### Structure

* Id (Guid)
* ContentTypeId (Guid)
* Title (string)
* Author (string)
* Content (string)
* CorrelationId (Guid)
* Version (int)
* ApprovalId (Guid)

### Behaviour

* A ContentItem represents a single version of a logical item
* CorrelationId groups all versions of the same item
* The first version MUST be Version 1
* Updates MUST create a new version in a draft state
* A new version MUST NOT be created until the current version is approved or rejected
* ApprovalId determines the current state of the item

---

## ContentType

Defines the classification of content.

### Structure

* Id (Guid)
* Name (string)

### Behaviour

* Used to categorise ContentItems (e.g., Blog, Quote, Story)
* Managed by administrators
* Not subject to approval

---

## ContentItemSetting (Policy Control)

Defines behaviour and visibility rules for content and related features.

### Structure

* Id (Guid)
* EntityType (string)
* EntityId (optional)

### Behaviour

* Acts as a policy layer controlling UI and feature behaviour
* When EntityId is not provided, the setting acts as a default
* When EntityId is provided, it overrides the default

### Capabilities Controlled

* Tags (creation, approval requirement, visibility)
* Reactions (availability, restrictions, approval requirement, visibility)
* Links (availability, approval requirement, visibility)
* Attachments (availability, approval requirement, visibility)
* Comments (availability, approval requirement, visibility)

---

## Approval (Generic Workflow)

Provides a reusable mechanism to govern all entities.

### Approval

* Id (Guid)
* EntityType (string)
* EntityId (Guid)
* StatusId (Pending, Approved, Rejected)

### ApprovalComment

* Id (Guid)
* ApprovalId
* Comment

### ApprovalReview

* Id (Guid)
* ApprovalId
* StatusId

### ApprovalSettings

Defines rules for how approvals are evaluated.

* RequiredApprovals (int)
* AllowSelfApproval (bool)
* BlockOnReject (bool)
* RequireReapprovalOnChange (bool)
* AutoApproveIfThresholdMet (bool)
* MustBeInRoleToApprove (bool)

### ApprovalSettingsRole

* Id (Guid)
* ApprovalSettingId (Guid)
* RoleName (string)

### Behaviour

* All governed entities MUST use the approval system
* Approval outcomes are determined by rules defined in ApprovalSettings
* Approval logic MUST be consistent and reusable across entity types

---

## Supporting Features

### Tag

* Id (Guid), Name
* Used to categorise and describe content
* Subject to approval

### Reaction

* Id (Guid), Name, UnicodeEmoji
* Represents user sentiment (e.g., Like, Love)
* Subject to approval

### Link

* Id (Guid), Name, LinkType, Uri
* Represents external references (e.g., URL, video, document)
* Subject to approval

### Comment

* Id (Guid), Content
* Enables discussion on content
* Subject to approval (configurable)

### BibleReference

* Id (Guid), Reference, Translation, Scripture
* Stores scripture content for association

### Attachment

* Id (Guid), Name, BlobUri, Hash
* Stores locally managed files

---

## Association

Links ContentItems to other entities.

### Structure

* Id (Guid)
* Scope (AllVersions, ThisVersionOnly)
* ContentItemId
* CorrelationId
* EntityType
* EntityId
* ApprovalId

### Behaviour

* Enables flexible relationships between content and other features
* Supports version-specific or cross-version associations
* Subject to approval

---

## Business Processes

### Content Creation

* A user creates or updates a ContentItem
* The ContentItem is stored
* An Approval is created or linked

### Review Process

* Reviewers identify items requiring approval
* Reviewers may:

  * Add comments
  * Approve
  * Reject

### Approval Evaluation

* The system evaluates all reviews against ApprovalSettings
* Approval is granted only when rules are satisfied
* Rejection may block further progression depending on rules

### Publication

* Only approved content becomes visible to general users

---

## Administration

Administrators manage:

* ContentTypes
* Approval rules and policies
* Roles and permissions
* System-level configurations

---

## Key Principles

* Content is versioned and never overwritten
* All meaningful actions are governed
* Behaviour is driven by explicit policy
* Approval is central to all workflows
* Visibility is controlled, not assumed

---
