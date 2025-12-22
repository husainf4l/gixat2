import 'package:flutter/foundation.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:graphql_flutter/graphql_flutter.dart';
import 'package:gql_http_link/gql_http_link.dart';

class GraphQLConfig {
  // Use environment variables for endpoints
  static String get _endpoint {
    final endpoint = kDebugMode 
      ? dotenv.env['GRAPHQL_ENDPOINT'] ?? 'http://localhost:8002/graphql'
      : dotenv.env['GRAPHQL_ENDPOINT_PROD'] ?? 'https://gixat.com/graphql';
    
    if (kDebugMode) {
      print('🌐 GraphQL Endpoint: $endpoint');
    }
    return endpoint;
  }
  
  static const FlutterSecureStorage _storage = FlutterSecureStorage();
  static const Duration _timeout = Duration(seconds: 30);

  static HttpLink get httpLink => HttpLink(
    _endpoint,
    defaultHeaders: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    },
  );

  // Create a link with error handling
  static Link get _linkWithErrorHandling {
    return Link.function((request, [forward]) async* {
      try {
        yield* forward!(request).timeout(
          _timeout,
          onTimeout: (sink) {
            sink.addError(
              Exception(
                'Connection timeout. Please check your internet connection and try again.'
              ),
            );
          },
        );
      } catch (e) {
        if (kDebugMode) {
          print('GraphQL Error: $e');
        }
        rethrow;
      }
    });
  }

  static Future<AuthLink> get authLink async {
    final token = await _storage.read(key: 'auth_token');
    return AuthLink(
      getToken: () async => token != null ? 'Bearer $token' : null,
    );
  }

  static Future<GraphQLClient> getClient({bool withAuth = false}) async {
    Link link;

    if (withAuth) {
      final auth = await authLink;
      link = Link.from([
        _linkWithErrorHandling,
        auth,
        httpLink,
      ]);
    } else {
      link = Link.from([
        _linkWithErrorHandling,
        httpLink,
      ]);
    }

    return GraphQLClient(
      link: link,
      cache: GraphQLCache(
        store: HiveStore(),
        partialDataPolicy: PartialDataCachePolicy.accept,
      ),
      defaultPolicies: DefaultPolicies(
        query: Policies(
          fetch: FetchPolicy.cacheFirst,
          cacheReread: CacheRereadPolicy.mergeOptimistic,
        ),
        mutate: Policies(
          fetch: FetchPolicy.networkOnly,
        ),
      ),
    );
  }

  static ValueNotifier<GraphQLClient> clientFor({String? token}) {
    final authLink = AuthLink(
      getToken: () async => token != null ? 'Bearer $token' : null,
    );

    final link = Link.from([
      _linkWithErrorHandling,
      authLink,
      httpLink,
    ]);

    return ValueNotifier(
      GraphQLClient(
        link: link,
        cache: GraphQLCache(
          store: HiveStore(),
          partialDataPolicy: PartialDataCachePolicy.accept,
        ),
        defaultPolicies: DefaultPolicies(
          query: Policies(
            fetch: FetchPolicy.cacheFirst,
            cacheReread: CacheRereadPolicy.mergeOptimistic,
          ),
          mutate: Policies(
            fetch: FetchPolicy.networkOnly,
          ),
        ),
      ),
    );
  }
}
