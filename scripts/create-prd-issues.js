#!/usr/bin/env node

/**
 * Create GitHub Issues from PRD User Stories
 * 
 * This script implements the same logic as the GitHub Actions workflow
 * but can be run locally with GitHub CLI authentication.
 */

const fs = require('fs');
const { execSync } = require('child_process');
const path = require('path');

// Configuration
const PRD_FILE = 'prd.md';
const OWNER = 'two4suited';
const REPO = 'FlyballStats.com';

function parseUserStories() {
  if (!fs.existsSync(PRD_FILE)) {
    console.error(`Error: ${PRD_FILE} not found at repo root`);
    process.exit(1);
  }

  const md = fs.readFileSync(PRD_FILE, 'utf8');

  // Split into story blocks by headings like: ### 10.x Title
  const lines = md.split(/\r?\n/);
  const stories = [];
  let i = 0;
  
  while (i < lines.length) {
    const m = lines[i].match(/^###\s+10\.(\d+)\s+(.*)$/);
    if (!m) { i++; continue; }
    
    const idx = m[1];
    const titleText = m[2].trim();
    const start = i;
    i++;
    let end = lines.length;
    
    for (let j = i; j < lines.length; j++) {
      if (/^###\s+10\./.test(lines[j])) { 
        end = j; 
        break; 
      }
    }
    
    const block = lines.slice(start, end).join('\n');
    
    // Extract ID
    const idMatch = block.match(/-\s*ID:\s*(GH-\d+)/i);
    const id = idMatch ? idMatch[1].toUpperCase() : `GH-${idx.padStart(3, '0')}`;
    
    // Description
    const descMatch = block.match(/-\s*Description:\s*([\s\S]*?)(?:\n-\s*Acceptance criteria:|$)/i);
    const description = descMatch ? descMatch[1].trim() : '';
    
    // Acceptance criteria bullets following "- Acceptance criteria:" with lines starting by two-space dash
    const acMatch = block.match(/-\s*Acceptance criteria:\s*\n([\s\S]*)/i);
    let acBullets = [];
    if (acMatch) {
      const acSection = acMatch[1];
      for (const line of acSection.split('\n')) {
        const b = line.match(/^\s{2}-\s+(.*)$/);
        if (b) acBullets.push(b[1]);
      }
    }
    
    stories.push({ id, idx, titleText, description, acBullets });
    i = end;
  }

  return stories;
}

function createIssueBody(story) {
  const bodyLines = [];
  bodyLines.push(`ID: ${story.id}`);
  
  if (story.description) {
    bodyLines.push('');
    bodyLines.push('Description:');
    bodyLines.push(story.description);
  }
  
  if (story.acBullets.length) {
    bodyLines.push('');
    bodyLines.push('Acceptance criteria:');
    for (const b of story.acBullets) {
      bodyLines.push(`- ${b}`);
    }
  }
  
  bodyLines.push('');
  bodyLines.push('Source: prd.md');
  
  return bodyLines.join('\n');
}

function checkGitHubCLI() {
  try {
    execSync('gh --version', { stdio: 'ignore' });
    return true;
  } catch (error) {
    return false;
  }
}

function ensureLabels() {
  console.log('Ensuring required labels exist...');
  
  const labels = [
    { name: 'prd', color: '0E8A16', description: 'Product Requirements Document' },
    { name: 'user-story', color: '5319E7', description: 'User story from PRD' }
  ];

  for (const label of labels) {
    try {
      // Try to get the label
      execSync(`gh label view "${label.name}" --repo ${OWNER}/${REPO}`, { stdio: 'ignore' });
      console.log(`  ✓ Label "${label.name}" already exists`);
    } catch (error) {
      // Label doesn't exist, create it
      try {
        execSync(`gh label create "${label.name}" --color "${label.color}" --description "${label.description}" --repo ${OWNER}/${REPO}`, { stdio: 'ignore' });
        console.log(`  ✓ Created label "${label.name}"`);
      } catch (createError) {
        console.error(`  ✗ Failed to create label "${label.name}": ${createError.message}`);
      }
    }
  }
}

function checkDuplicateIssue(id) {
  try {
    const output = execSync(`gh issue list --search "in:title ${id}" --repo ${OWNER}/${REPO} --json number`, { encoding: 'utf8' });
    const issues = JSON.parse(output);
    return issues.length > 0;
  } catch (error) {
    console.warn(`Warning: Could not check for duplicate issue ${id}: ${error.message}`);
    return false;
  }
}

function createIssue(story) {
  const title = `[${story.id}] ${story.titleText}`;
  const body = createIssueBody(story);
  
  // Check for duplicates
  if (checkDuplicateIssue(story.id)) {
    console.log(`  ⏭ Skipping existing issue for ${story.id}`);
    return null;
  }

  try {
    // Create the issue
    const output = execSync(`gh issue create --title "${title}" --body "${body}" --label "prd,user-story" --repo ${OWNER}/${REPO}`, { encoding: 'utf8' });
    const issueUrl = output.trim();
    console.log(`  ✓ Created ${story.id}: ${issueUrl}`);
    return issueUrl;
  } catch (error) {
    console.error(`  ✗ Failed to create issue for ${story.id}: ${error.message}`);
    return null;
  }
}

function main() {
  console.log('Creating GitHub Issues from PRD User Stories');
  console.log('='.repeat(50));

  // Check prerequisites
  if (!checkGitHubCLI()) {
    console.error('Error: GitHub CLI (gh) is not installed or not in PATH');
    console.error('Please install it from: https://github.com/cli/cli');
    process.exit(1);
  }

  // Parse user stories
  console.log(`\nParsing user stories from ${PRD_FILE}...`);
  const stories = parseUserStories();
  console.log(`Found ${stories.length} user stories to process`);

  // Ensure required labels exist
  ensureLabels();

  // Create issues
  console.log('\nCreating GitHub issues...');
  const created = [];
  
  for (const story of stories) {
    const url = createIssue(story);
    if (url) {
      created.push(url);
    }
  }

  // Summary
  console.log('\n' + '='.repeat(50));
  console.log(`Summary: Created ${created.length} out of ${stories.length} issues`);
  
  if (created.length > 0) {
    console.log('\nCreated issues:');
    created.forEach(url => console.log(`  - ${url}`));
  }

  if (created.length < stories.length) {
    console.log(`\nNote: ${stories.length - created.length} issues were skipped (likely already exist)`);
  }
}

// Run the script
if (require.main === module) {
  main();
}

module.exports = { parseUserStories, createIssueBody };