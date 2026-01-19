---
name: featbit-skills
description: FeatBit Skill for Feature Flag Management and Evaluation. (1) CRUD operations on feature flags. (2) Evaluate flags for users.
author: featbit-skills-team
---

# FeatBit Skill for Feature Flag Management and Evaluation

This skill allows you to interact with the FeatBit feature flags. This skills provide two main capabilities:

1) CRUD Operations on Feature Flags: You can use it to list, create, read, update, and delete feature flags in the given FeatBit project and environment.
2) Evaluate Feature Flags results for a given user or context.

All the operations and evaluations are performed using the scripts under the same directory where this skill is located.

FeatBit MCP server is not required for this skill, but you may call it to get help on managing feature flags.

## When to use this skill

Use this skill when you need to manage feature flags programmatically or evaluate feature flags for users in your applications. 

It is particularly useful for when you want Agent to help you with tasks such as:
- Add new feature flags to a project's environment.
- Update existing feature flags with new configurations.
- Delete feature flags that are no longer needed.
- List all feature flags in a specific project and environment.
- Retrieve details of a specific feature flag.
- Evaluate feature flags for specific users to determine their access to features.

It is also super useful when you want to achieve a complex business logic that involves a multiple steps of feature flag management and evaluation. For example, you want to create parent feature flag then use its return value as a custom property when creating child feature flags. Then you set evaulation rules based on the custom property that equals to the parent flag's value. In this example, the skill can help you orchestrate the entire process seamlessly.

## List all feature flags

Use `[list feature flags script](./list-feature-flags/script.js)` to list all feature flags in a given project and environment. 

See detailed instructions in the `[list feature flags instruction](./list-feature-flags/instruction.md)` file.

## Enable and Disable a Feature Flag

Use `./enable-disable-feature-flag/script.js` to disable or enable a feature flag in a given project and environment.

See detailed instructions in the `[list feature flags instruction](./enable-disable-feature-flag/instruction.md)` file.
