import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:graphql_flutter/graphql_flutter.dart';

class GraphQLConfig {
  static const String _endpoint = 'https://gixat.com/graphql/';
  static const FlutterSecureStorage _storage = FlutterSecureStorage();

  static HttpLink get httpLink => HttpLink(_endpoint);

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
      link = auth.concat(httpLink);
    } else {
      link = httpLink;
    }

    return GraphQLClient(
      link: link,
      cache: GraphQLCache(store: InMemoryStore()),
    );
  }

  static ValueNotifier<GraphQLClient> clientFor({String? token}) {
    final authLink = AuthLink(
      getToken: () async => token != null ? 'Bearer $token' : null,
    );

    final link = authLink.concat(httpLink);

    return ValueNotifier(
      GraphQLClient(
        link: link,
        cache: GraphQLCache(store: InMemoryStore()),
      ),
    );
  }
}
