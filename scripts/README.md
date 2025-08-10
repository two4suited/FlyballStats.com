# Scripts Directory

This directory contains utility scripts for the FlyballStats.com project.

## create-prd-issues.js

Creates GitHub issues from the PRD user stories defined in `prd.md`.

### Prerequisites

- [GitHub CLI](https://github.com/cli/cli) installed and authenticated
- Repository access to create issues and labels

### Usage

From the repository root:

```bash
node scripts/create-prd-issues.js
```

### What it does

1. Parses `prd.md` to extract user stories from section "## 10. User stories"
2. Creates the required labels "prd" and "user-story" if they don't exist
3. For each user story:
   - Checks if an issue with the same ID already exists
   - Creates a new issue if none exists
   - Uses format: `[GH-XXX] {story title}`
   - Includes ID, Description, Acceptance criteria, and Source reference
   - Applies "prd" and "user-story" labels
4. Provides a summary of created issues

### Expected Output

The script will create 21 issues for user stories GH-001 through GH-021 from the PRD.