const {env} = require('process');

const target = env["services__fablecraft-server__https__0"]
  || env["services__fablecraft-server__http__0"]
  || 'https://localhost:7132';

console.log('===========================================');
console.log('Proxy Configuration:');
console.log('Target backend URL:', target);
console.log('===========================================');

const PROXY_CONFIG = [
  {
    context: [
      "/api",
      "/visualization"
    ],
    target,
    secure: false,
    changeOrigin: true,
    logLevel: 'debug'
  }
]

module.exports = PROXY_CONFIG;
