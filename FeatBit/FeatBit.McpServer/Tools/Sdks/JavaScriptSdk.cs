namespace FeatBit.McpServer.Tools.Sdks;

public static class JavaScriptSdk
{
    public static SdkInfo Info => new(
        Platform: "JavaScript/TypeScript",
        Type: "Client",
        PackageName: "@featbit/js-client-sdk",
        InstallCommand: "npm install @featbit/js-client-sdk",
        Repository: "https://github.com/featbit/featbit-js-client-sdk",
        Documentation: "https://docs.featbit.co/sdk/client-side-sdks/javascript"
    );

    public static string GenerateIntegrationCode(string envSecret, string streamingUrl, string eventUrl, string topic = "all")
    {
        return topic.ToLower() switch
        {
            "basic" => GetBasicSetup(envSecret, streamingUrl, eventUrl),
            "advanced-config" => GetAdvancedConfig(envSecret, streamingUrl, eventUrl),
            "evaluation" => GetFlagEvaluation(),
            "evaluation-detail" => GetDetailedEvaluation(),
            "events" => GetEventHandling(),
            "user-management" => GetUserManagement(),
            "experimentation" => GetExperimentation(),
            "lifecycle" => GetLifecycleManagement(),
            "practical-examples" => GetPracticalExamples(),
            "all" or _ => GetComprehensiveGuide(envSecret, streamingUrl, eventUrl)
        };
    }

    private static string GetBasicSetup(string envSecret, string streamingUrl, string eventUrl)
    {
        return $$$"""
        // ============================================================================
        // FeatBit JavaScript SDK - Basic Setup
        // ============================================================================
        
        // 1. Installation
        // npm install @featbit/js-client-sdk
        
        import { FbClientBuilder, UserBuilder } from '@featbit/js-client-sdk';
        
        // 2. Define User
        const user = new UserBuilder('user-unique-id')
          .name('User Name')
          .custom('email', 'user@example.com')
          .custom('country', 'US')
          .build();
        
        // 3. Initialize FeatBit Client (Streaming Mode - Recommended)
        const fbClient = new FbClientBuilder()
          .sdkKey('{{{envSecret}}}')
          .user(user)
          .streamingUri('{{{streamingUrl}}}')
          .eventsUri('{{{eventUrl}}}')
          .build();
        
        // 4. Wait for Initialization and Evaluate Flags
        (async () => {
          try {
            await fbClient.waitForInitialization();
            console.log('✅ SDK initialized successfully');
            
            // Evaluate feature flags
            const isEnabled = fbClient.boolVariation('feature-flag-key', false);
            
            if (isEnabled) {
              console.log('Feature enabled');
              // Execute new feature code
            } else {
              console.log('Feature disabled');
              // Execute old code
            }
            
          } catch (error) {
            console.error('Initialization failed:', error);
          }
        })();
        
        // Alternative: Using event listener
        fbClient.on('ready', () => {
          console.log('SDK ready');
          const flagValue = fbClient.boolVariation('feature-flag-key', false);
        });
        """;
    }

    private static string GetAdvancedConfig(string envSecret, string streamingUrl, string eventUrl)
    {
        return $$$"""
        // ============================================================================
        // FeatBit JavaScript SDK - Advanced Configuration
        // ============================================================================
        
        import { 
          FbClientBuilder, 
          UserBuilder, 
          DataSyncModeEnum,
          BasicLogger 
        } from '@featbit/js-client-sdk';
        
        const user = new UserBuilder('user-id').name('User').build();
        
        // 1. Streaming Mode with Custom Logger
        const logger = new BasicLogger({
          level: 'debug', // 'debug', 'info', 'warn', 'error', 'none'
          destination: console.log
        });
        
        const fbClient = new FbClientBuilder()
          .sdkKey('{{{envSecret}}}')
          .user(user)
          .streamingUri('{{{streamingUrl}}}')
          .eventsUri('{{{eventUrl}}}')
          .startWaitTime(5000)     // Max wait time for initialization (ms)
          .logger(logger)          // Custom logger
          .build();
        
        // 2. Polling Mode Configuration
        const fbClientPolling = new FbClientBuilder()
          .sdkKey('{{{envSecret}}}')
          .user(user)
          .dataSyncMode(DataSyncModeEnum.POLLING)
          .pollingUri('{{{streamingUrl}}}'.replace('ws://', 'http://').replace('wss://', 'https://'))
          .eventsUri('{{{eventUrl}}}')
          .pollingInterval(10000)  // Poll every 10 seconds
          .build();
        
        // 3. Offline Mode with Bootstrap
        const bootstrapFlags = [
          {
            id: 'feature-flag-key',
            variation: 'true',
            variationType: 'boolean'
          },
          {
            id: 'theme-flag',
            variation: 'dark',
            variationType: 'string'
          }
        ];
        
        const fbClientOffline = new FbClientBuilder()
          .sdkKey('{{{envSecret}}}')
          .user(user)
          .offline(true)           // Work completely offline
          .bootstrap(bootstrapFlags)
          .build();
        
        // 4. Disable Events Collection
        const fbClientNoEvents = new FbClientBuilder()
          .sdkKey('{{{envSecret}}}')
          .user(user)
          .streamingUri('{{{streamingUrl}}}')
          .eventsUri('{{{eventUrl}}}')
          .disableEvents(true)     // No analytics/events sent
          .build();
        """;
    }

    private static string GetFlagEvaluation()
    {
        return """
        // ============================================================================
        // FeatBit JavaScript SDK - Flag Evaluation
        // ============================================================================
        
        async function evaluateFlags() {
          await fbClient.waitForInitialization();
          
          // 1. Boolean Flags
          const isFeatureEnabled = fbClient.boolVariation('new-checkout', false);
          const darkModeEnabled = fbClient.boolVariation('dark-mode', false);
          
          // 2. String Flags
          const theme = fbClient.stringVariation('theme', 'light');
          const apiEndpoint = fbClient.stringVariation('api-endpoint', '/api/v1');
          
          // 3. Number Flags
          const maxRetries = fbClient.numberVariation('max-retries', 3);
          const timeout = fbClient.numberVariation('request-timeout', 5000);
          const pageSize = fbClient.numberVariation('page-size', 20);
          
          // 4. JSON/Object Flags
          const config = fbClient.jsonVariation('app-config', { 
            timeout: 5000, 
            retries: 3 
          });
          
          // 5. Generic Variation (auto-detects type)
          const genericValue = fbClient.variation('any-flag', 'default');
          
          console.log('Flags:', {
            isFeatureEnabled,
            theme,
            maxRetries,
            config
          });
        }
        
        // Get all flags at once
        async function getAllFlags() {
          const allVariations = await fbClient.getAllVariations();
          console.log('All flags:', allVariations);
          return allVariations;
        }
        """;
    }

    private static string GetDetailedEvaluation()
    {
        return """
        // ============================================================================
        // FeatBit JavaScript SDK - Detailed Flag Evaluation
        // ============================================================================
        
        async function evaluateWithDetails() {
          await fbClient.waitForInitialization();
          
          // Get evaluation details for debugging/auditing
          const detail = fbClient.boolVariationDetail('premium-feature', false);
          
          console.log('Flag Evaluation Detail:', {
            value: detail.value,      // true/false
            reason: detail.reason,    // Why this value was returned
            kind: detail.kind,        // Evaluation kind
            flagKey: detail.flagKey,  // Flag identifier
            name: detail.name         // Flag name
          });
          
          // Type-specific detail methods
          const stringDetail = fbClient.stringVariationDetail('theme', 'light');
          const numberDetail = fbClient.numberVariationDetail('timeout', 5000);
          const jsonDetail = fbClient.jsonVariationDetail('config', {});
          const variationDetail = fbClient.variationDetail('any-flag', 'default');
        }
        """;
    }

    private static string GetEventHandling()
    {
        return """
        // ============================================================================
        // FeatBit JavaScript SDK - Event Handling
        // ============================================================================
        
        // 1. Listen to ALL flag changes
        fbClient.on('update', (flagKeys) => {
          console.log('🔄 Flags updated:', flagKeys);
          flagKeys.forEach(key => {
            const newValue = fbClient.variation(key, null);
            console.log(`  - ${key}: ${newValue}`);
          });
        });
        
        // 2. Listen to SPECIFIC flag changes
        fbClient.on('update:premium-feature', (key) => {
          const isPremium = fbClient.boolVariation(key, false);
          console.log(`🔄 Premium feature changed to: ${isPremium}`);
          
          // Update UI based on flag change
          if (isPremium) {
            showPremiumFeatures();
          } else {
            hidePremiumFeatures();
          }
        });
        
        // 3. Ready event
        fbClient.on('ready', () => {
          console.log('✅ SDK is ready');
        });
        
        // 4. Failed event
        fbClient.on('failed', (error) => {
          console.error('❌ SDK failed:', error);
        });
        
        // Helper functions (implement based on your needs)
        function showPremiumFeatures() { /* ... */ }
        function hidePremiumFeatures() { /* ... */ }
        """;
    }

    private static string GetUserManagement()
    {
        return """
        // ============================================================================
        // FeatBit JavaScript SDK - User Identity Management
        // ============================================================================
        
        // 1. Switch user (e.g., after login)
        async function switchUser(userId, userAttributes) {
          const newUser = new UserBuilder(userId)
            .name(userAttributes.name)
            .custom('email', userAttributes.email)
            .custom('tier', userAttributes.tier)
            .custom('country', userAttributes.country)
            .build();
          
          await fbClient.identify(newUser);
          console.log('👤 User switched to:', userId);
          
          // Re-evaluate flags for new user
          const userFeatures = fbClient.boolVariation('user-specific-feature', false);
        }
        
        // 2. Handle user logout
        async function handleLogout() {
          const anonymousUser = new UserBuilder('anonymous-' + Date.now())
            .name('Anonymous User')
            .build();
          
          await fbClient.identify(anonymousUser);
        }
        
        // 3. Update user attributes without changing ID
        async function updateUserAttributes(attributes) {
          const currentUserId = 'current-user-id'; // Get from your state
          const updatedUser = new UserBuilder(currentUserId)
            .name(attributes.name)
            .custom('tier', attributes.tier)
            .custom('country', attributes.country)
            .build();
          
          await fbClient.identify(updatedUser);
        }
        """;
    }

    private static string GetExperimentation()
    {
        return """
        // ============================================================================
        // FeatBit JavaScript SDK - A/B Testing & Experimentation
        // ============================================================================
        
        async function runExperiment() {
          await fbClient.waitForInitialization();
          
          // Evaluate experiment flag
          const experimentVariant = fbClient.stringVariation('checkout-experiment', 'control');
          
          // Show different variants
          if (experimentVariant === 'variant-a') {
            showVariantA();
            
            // Track conversion event
            document.querySelector('#purchase-btn')?.addEventListener('click', () => {
              fbClient.track('purchase-completed', 99.99); // metric value
              console.log('📊 Conversion tracked for variant-a');
            });
            
          } else if (experimentVariant === 'variant-b') {
            showVariantB();
            
            document.querySelector('#purchase-btn')?.addEventListener('click', () => {
              fbClient.track('purchase-completed', 99.99);
              console.log('📊 Conversion tracked for variant-b');
            });
          } else {
            showControlVariant();
          }
        }
        
        // Track custom events for experimentation
        fbClient.track('page-viewed');                    // Default metric value = 1
        fbClient.track('button-clicked', 1.0);            // Custom metric value
        fbClient.track('video-watched', 120);             // 120 seconds watched
        fbClient.track('purchase-amount', 99.99);         // Revenue tracking
        
        // Helper functions (implement based on your needs)
        function showVariantA() { /* ... */ }
        function showVariantB() { /* ... */ }
        function showControlVariant() { /* ... */ }
        """;
    }

    private static string GetLifecycleManagement()
    {
        return """
        // ============================================================================
        // FeatBit JavaScript SDK - Lifecycle Management
        // ============================================================================
        
        // 1. Flush events immediately (before page unload)
        window.addEventListener('beforeunload', async () => {
          await fbClient.flush(); // Send pending events
        });
        
        // 2. Manual flush with callback
        async function flushEvents() {
          const success = await fbClient.flush((result) => {
            console.log('Events flushed:', result);
          });
          console.log('Flush completed:', success);
        }
        
        // 3. Close/cleanup SDK (app shutdown)
        async function shutdownSDK() {
          await fbClient.close();
          console.log('🔌 SDK closed and cleaned up');
        }
        
        // 4. Check if SDK is initialized
        if (fbClient.initialized()) {
          console.log('SDK is ready');
        } else {
          console.log('SDK is still initializing');
        }
        
        // 5. Error handling
        async function robustFlagEvaluation() {
          try {
            // SDK works even if not fully initialized (returns default)
            const feature = fbClient.boolVariation('new-feature', false);
            
            if (fbClient.initialized()) {
              console.log('✅ Using live flag value:', feature);
            } else {
              console.log('⚠️ Using default value:', feature);
            }
            
          } catch (error) {
            console.error('Error evaluating flag:', error);
            return false; // Fail gracefully with default
          }
        }
        """;
    }

    private static string GetPracticalExamples()
    {
        return """
        // ============================================================================
        // FeatBit JavaScript SDK - Practical Usage Examples
        // ============================================================================
        
        // 1. Feature Toggle
        async function renderUI() {
          await fbClient.waitForInitialization();
          
          if (fbClient.boolVariation('new-ui', false)) {
            renderNewUI();
          } else {
            renderOldUI();
          }
        }
        
        // 2. Configuration Management
        async function loadConfig() {
          const config = fbClient.jsonVariation('app-config', {
            apiTimeout: 5000,
            maxRetries: 3,
            enableCache: true
          });
          
          // Use config values
          fetch('/api/data', { timeout: config.apiTimeout });
          return config;
        }
        
        // 3. Gradual Rollout with Targeting
        async function checkAccessToFeature() {
          const hasAccess = fbClient.boolVariation('beta-feature', false);
          
          if (hasAccess) {
            console.log('✅ User has access to beta feature');
            enableBetaFeature();
          } else {
            console.log('⏸️ User not in rollout group');
          }
        }
        
        // 4. A/B Test with Multiple Variants
        async function showRecommendationAlgorithm() {
          const algorithm = fbClient.stringVariation('recommendation-algo', 'default');
          
          switch (algorithm) {
            case 'collaborative':
              return getCollaborativeRecommendations();
            case 'content-based':
              return getContentBasedRecommendations();
            case 'hybrid':
              return getHybridRecommendations();
            default:
              return getDefaultRecommendations();
          }
        }
        
        // 5. Dynamic Theming
        async function applyDynamicTheme() {
          const theme = fbClient.stringVariation('app-theme', 'light');
          document.body.className = `theme-${theme}`;
          
          // Listen for theme changes
          fbClient.on('update:app-theme', (key) => {
            const newTheme = fbClient.stringVariation(key, 'light');
            document.body.className = `theme-${newTheme}`;
          });
        }
        
        // Helper functions (implement based on your needs)
        function renderNewUI() { /* ... */ }
        function renderOldUI() { /* ... */ }
        function enableBetaFeature() { /* ... */ }
        function getCollaborativeRecommendations() { /* ... */ }
        function getContentBasedRecommendations() { /* ... */ }
        function getHybridRecommendations() { /* ... */ }
        function getDefaultRecommendations() { /* ... */ }
        """;
    }

    private static string GetComprehensiveGuide(string envSecret, string streamingUrl, string eventUrl)
    {
        return $$$"""
        // ============================================================================
        // FeatBit JavaScript SDK - Comprehensive Integration Guide
        // ============================================================================
        
        // Available topics (use 'topic' parameter to get specific sections):
        // - basic: Basic setup and initialization
        // - advanced-config: Advanced configuration options
        // - evaluation: Flag evaluation examples
        // - evaluation-detail: Detailed evaluation with metadata
        // - events: Real-time event handling
        // - user-management: User identity management
        // - experimentation: A/B testing and experiments
        // - lifecycle: SDK lifecycle management
        // - practical-examples: Real-world usage patterns
        
        {{{GetBasicSetup(envSecret, streamingUrl, eventUrl)}}}
        
        {{{GetAdvancedConfig(envSecret, streamingUrl, eventUrl)}}}
        
        {{{GetFlagEvaluation()}}}
        
        {{{GetDetailedEvaluation()}}}
        
        {{{GetEventHandling()}}}
        
        {{{GetUserManagement()}}}
        
        {{{GetExperimentation()}}}
        
        {{{GetLifecycleManagement()}}}
        
        {{{GetPracticalExamples()}}}
        """;
    }
}
