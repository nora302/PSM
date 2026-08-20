import 'dart:convert';

import 'package:http/http.dart' as http;

import 'api_service.dart';

class LoginResult {
  final bool erfolgreich;
  final String? fehler;
  final String? token;
  final List<String> rollen;

  const LoginResult({
    required this.erfolgreich,
    this.fehler,
    this.token,
    this.rollen = const [],
  });
}

class AuthService {
  static String? token;

  Future<LoginResult> login({
    required String benutzername,
    required String passwort,
  }) async {
    try {
      final response = await http.post(
        Uri.parse(
          '${ApiService.baseUrl}/api/auth/login',
        ),
        headers: {
          'Content-Type': 'application/json',
        },
        body: jsonEncode({
          'benutzername': benutzername,
          'passwort': passwort,
        }),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);

        token = data['token'];

        final benutzer = data['benutzer'];

        final rollen = List<String>.from(
          benutzer['rollen'] ?? [],
        );

        return LoginResult(
          erfolgreich: true,
          token: token,
          rollen: rollen,
        );
      }

      if (response.statusCode == 401) {
        return const LoginResult(
          erfolgreich: false,
          fehler:
          'Benutzername oder Passwort ist falsch.',
        );
      }

      return LoginResult(
        erfolgreich: false,
        fehler:
        'Serverfehler: ${response.statusCode}',
      );
    } catch (e) {
      return const LoginResult(
        erfolgreich: false,
        fehler:
        'Verbindung zum PSM-Server nicht möglich.',
      );
    }
  }

  static void logout() {
    token = null;
  }

  static bool get isAuthenticated {
    return token != null;
  }
}