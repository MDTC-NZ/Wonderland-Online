/**
 * Server Configuration Loader
 * Dynamically loads the correct server configuration
 */

let CONFIG = {
    // Production server
    server: {
        domain: "wlo.kiaoragaming.xyz",
        game_port: 5423,
        api_port: 5423,
        use_https: true
    },
    
    // Development/local server (fallback)
    local: {
        domain: "localhost",
        game_port: 6414,
        api_port: 8090,
        use_https: false
    },
    
    // Determine which config to use
    isProduction: function() {
        const hostname = window.location.hostname;
        return hostname !== 'localhost' && hostname !== '127.0.0.1';
    },
    
    // Get API base URL
    getApiBase: function() {
        const config = this.isProduction() ? this.server : this.local;
        const protocol = config.use_https ? 'https' : 'http';
        const port = config.api_port;
        
        // If port is standard for protocol, omit it
        if ((config.use_https && port === 443) || (!config.use_https && port === 80)) {
            return `${protocol}://${config.domain}/api`;
        }
        return `${protocol}://${config.domain}:${port}/api`;
    },
    
    // Get game server info
    getGameServer: function() {
        const config = this.isProduction() ? this.server : this.local;
        const protocol = config.use_https ? 'https' : 'http';
        const port = config.game_port;
        
        return {
            domain: config.domain,
            port: port,
            url: protocol + '://' + config.domain + ':' + port
        };
    },
    
    // Get current configuration
    getCurrent: function() {
        return this.isProduction() ? this.server : this.local;
    }
};

// Auto-detect and log config
console.log('🎮 Wonderland Online Configuration');
console.log('Environment: ' + (CONFIG.isProduction() ? 'PRODUCTION' : 'DEVELOPMENT'));
console.log('API Base: ' + CONFIG.getApiBase());
console.log('Game Server: ' + CONFIG.getGameServer().url);
