/**
 * Get flag list of an environment
 * Get the list of flags of a particular environment from FeatBit API.
 * 
 * API Endpoint: https://app-api.featbit.co/api/v1/envs/{envId}/feature-flags
 */

async function getFlagListOfEnvironment(params = {}) {
    const {
        envId,
        accessToken,
        organization,
        queryParams = {}
    } = params;

    // Validate required parameters
    if (!envId) {
        throw new Error('envId is required');
    }

    if (!accessToken) {
        throw new Error('accessToken is required (JWT Bearer token or API Key)');
    }

    // Build query string
    const query = new URLSearchParams();
    
    if (queryParams.Name) {
        query.append('Name', queryParams.Name);
    }
    
    if (queryParams.Tags && Array.isArray(queryParams.Tags)) {
        queryParams.Tags.forEach(tag => query.append('Tags', tag));
    }
    
    if (queryParams.IsEnabled !== undefined) {
        query.append('IsEnabled', queryParams.IsEnabled);
    }
    
    if (queryParams.IsArchived !== undefined) {
        query.append('IsArchived', queryParams.IsArchived);
    }
    
    if (queryParams.SortBy) {
        query.append('SortBy', queryParams.SortBy);
    }
    
    if (queryParams.PageIndex !== undefined) {
        query.append('PageIndex', queryParams.PageIndex);
    }
    
    if (queryParams.PageSize !== undefined) {
        query.append('PageSize', queryParams.PageSize);
    }

    const queryString = query.toString();
    const url = `https://app-api.featbit.co/api/v1/envs/${envId}/feature-flags${queryString ? '?' + queryString : ''}`;

    // Prepare headers
    const headers = {
        'Content-Type': 'application/json'
    };

    // Add authorization header
    // Support both JWT Bearer token and API Key formats
    if (accessToken.startsWith('api-')) {
        headers['Authorization'] = accessToken;
    } else {
        headers['Authorization'] = `Bearer ${accessToken}`;
    }

    // Add organization header if provided
    if (organization) {
        headers['Organization'] = organization;
    }

    try {
        const response = await fetch(url, {
            method: 'GET',
            headers: headers
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`API request failed with status ${response.status}: ${errorText}`);
        }

        const data = await response.json();
        return data;
    } catch (error) {
        throw new Error(`Failed to get flag list: ${error.message}`);
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { getFlagListOfEnvironment };
}

// Example usage (for testing)
if (require.main === module) {
    // Read from environment variables or command line arguments
    const envId = process.env.FEATBIT_ENV_ID || process.argv[2];
    const accessToken = process.env.FEATBIT_ACCESS_TOKEN || process.argv[3];
    const organization = process.env.FEATBIT_ORGANIZATION || process.argv[4];

    if (!envId || !accessToken) {
        console.error('Usage: node get-flag-list-of-an-environment.js <envId> <accessToken> [organization]');
        console.error('Or set environment variables: FEATBIT_ENV_ID, FEATBIT_ACCESS_TOKEN, FEATBIT_ORGANIZATION');
        process.exit(1);
    }

    // Example with query parameters
    getFlagListOfEnvironment({
        envId: envId,
        accessToken: accessToken,
        organization: organization,
        queryParams: {
            PageIndex: 0,
            PageSize: 50,
            IsArchived: false
        }
    })
    .then(result => {
        console.log('Success! Feature flags retrieved:');
        console.log(JSON.stringify(result, null, 2));
    })
    .catch(error => {
        console.error('Error:', error.message);
        process.exit(1);
    });
}
