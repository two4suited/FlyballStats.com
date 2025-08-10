# PRD Issues Creation Guide

This document explains how to create GitHub issues from the PRD user stories.

## Option 1: Trigger the Existing Workflow (Recommended)

The repository already contains a GitHub Actions workflow at `.github/workflows/create-prd-issues.yml` that implements all the required functionality:

1. **Manual Trigger**: Go to the GitHub repository → Actions tab → "Create PRD Issues" workflow → Click "Run workflow"
2. **The workflow will**:
   - Parse `prd.md` from the repository root
   - Extract all 21 user stories from section "## 10. User stories"
   - Create GitHub issues with the format "[GH-XXX] {story title}"
   - Include ID, Description, and Acceptance criteria as bullets
   - Add labels "prd" and "user-story"
   - Skip duplicates if issues with the same ID already exist
   - Provide a summary of created issues

## Option 2: Using Local Script with GitHub CLI (Alternative)

If you have GitHub CLI installed and authenticated:

```bash
# Navigate to repository root
cd /path/to/FlyballStats.com

# Install GitHub CLI if not already installed
# See: https://github.com/cli/cli

# Authenticate with GitHub
gh auth login

# Run the issue creation script
node scripts/create-prd-issues.js
```

The script will:
- Parse the PRD file and extract all 21 user stories
- Create required labels if they don't exist
- Check for duplicate issues before creating new ones
- Create issues with proper formatting and labels
- Provide a summary of created issues

## Expected Issues to be Created

The following 21 issues will be created from the PRD user stories:

- [GH-001] upload schedule CSV
- [GH-002] validate and re-upload CSV
- [GH-003] configure rings and colors
- [GH-004] choose scheduling mode
- [GH-005] assign races to rings manually
- [GH-006] automatic odd/even assignment (2 rings)
- [GH-007] automatic next-up assignment (>2 rings)
- [GH-008] mark race done and advance
- [GH-009] correct ring state mistakes
- [GH-010] view live board
- [GH-011] authenticate with Entra ID
- [GH-012] claim a team
- [GH-013] receive notifications for team
- [GH-014] manage multiple tournaments
- [GH-015] archive tournament and view history
- [GH-016] error handling for CSV issues
- [GH-017] handle offline viewers
- [GH-018] authorization and roles
- [GH-019] real-time performance
- [GH-020] health and observability
- [GH-021] mark team as GHOST

## Issue Format

Each issue follows this format:

**Title**: `[GH-XXX] {story title}`

**Body**:
```
ID: GH-XXX

Description:
{User story description}

Acceptance criteria:
- {Criterion 1}
- {Criterion 2}
- ...

Source: prd.md
```

**Labels**: `prd`, `user-story`

## Verification

After running the workflow:
1. Check the GitHub repository issues tab
2. Verify all 21 issues are created with correct titles
3. Confirm each issue has the proper labels
4. Review the workflow summary for any errors or skipped issues