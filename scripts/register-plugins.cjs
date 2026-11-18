'use strict';

// Sparkle owns authentication and deployment. This launcher only resolves its
// pinned NuGet executable and forwards CLI arguments without shell interpolation.
const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const root = path.resolve(__dirname, '..');
const pluginDirectory = path.join(root, 'src', 'Company.Crm.Plugins');
const locationFile = path.join(root, 'artifacts', 'tools', 'spkl.path');

if (!fs.existsSync(locationFile)) {
    console.error('Run npm run build first.');
    process.exit(1);
}
const executable = fs.readFileSync(locationFile, 'utf8').trim();
if (!executable || !fs.existsSync(executable)) {
    console.error('The pinned Sparkle executable is missing. Run npm run build.');
    process.exit(1);
}

// Reject a missing local artifact before Sparkle can prompt for a connection.
const args = process.argv.slice(2);
const profileArgument = args.find(value => /^\/p:/i.test(value));
const profileName = profileArgument ? profileArgument.slice(3) : 'default';
const configuration = JSON.parse(fs.readFileSync(path.join(pluginDirectory, 'spkl.json'), 'utf8'));
const profiles = configuration.plugins.filter(profile =>
    (profile.profile || 'default').split(',').map(name => name.trim().toLowerCase())
        .includes(profileName.toLowerCase()));
if (profiles.length !== 1) {
    console.error('Expected one Sparkle plugin configuration for profile: ' + profileName);
    process.exit(1);
}
const artifact = path.resolve(pluginDirectory, profiles[0].assemblypath);
if (!fs.existsSync(artifact)) {
    console.error('Missing plugin DLL. Build the selected profile before registration: ' + profileName);
    process.exit(1);
}

const result = spawnSync(executable, ['plugins', pluginDirectory, ...args], {
    cwd: pluginDirectory,
    stdio: 'inherit',
    shell: false
});
if (result.error) {
    console.error('Could not start Sparkle: ' + result.error.message);
    process.exit(1);
}
process.exit(result.status === null ? 1 : result.status);
