const fs = require('fs');
const path = require('path');

const apiUrl = process.env['services__fablecraft-server__https__0']
  || process.env['services__fablecraft-server__http__0']
  || process.env.API_URL
  || '';

const isProduction = process.argv.includes('--production');
const envFileName = isProduction ? 'environment.production.ts' : 'environment.ts';
const envFilePath = path.join(__dirname, 'src', 'environments', envFileName);

let envFileContent = fs.readFileSync(envFilePath, 'utf8');

envFileContent = envFileContent.replace(/'\$\{API_URL\}'/g, `'${apiUrl}'`);

fs.writeFileSync(envFilePath, envFileContent);
