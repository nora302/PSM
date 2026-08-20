import 'package:flutter/material.dart';

import 'admin_page.dart';
import 'auth_service.dart';
import 'hauswirtschaft_page.dart';
import 'home_page.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final TextEditingController _benutzernameController =
  TextEditingController();

  final TextEditingController _passwortController =
  TextEditingController();

  final AuthService _authService = AuthService();

  bool _isLoading = false;
  bool _passwortAnzeigen = false;

  String? _fehler;

  @override
  void dispose() {
    _benutzernameController.dispose();
    _passwortController.dispose();
    super.dispose();
  }

  Future<void> _anmelden() async {
    FocusScope.of(context).unfocus();

    final benutzername =
    _benutzernameController.text.trim();

    final passwort =
        _passwortController.text;

    if (benutzername.isEmpty || passwort.isEmpty) {
      setState(() {
        _fehler =
        'Bitte Benutzername und Passwort eingeben.';
      });

      return;
    }

    setState(() {
      _isLoading = true;
      _fehler = null;
    });

    try {
      final result = await _authService.login(
        benutzername: benutzername,
        passwort: passwort,
      );

      if (!mounted) {
        return;
      }

      if (!result.erfolgreich) {
        setState(() {
          _fehler = result.fehler ??
              'Anmeldung fehlgeschlagen.';
        });

        return;
      }

      final rollen = result.rollen;

      if (rollen.contains('Administrator')) {
        Navigator.of(context).pushAndRemoveUntil(
          MaterialPageRoute(
            builder: (_) => const AdminPage(),
          ),
              (route) => false,
        );

        return;
      }

      if (rollen.contains('Pflegekraft')) {
        Navigator.of(context).pushAndRemoveUntil(
          MaterialPageRoute(
            builder: (_) => const HomePage(),
          ),
              (route) => false,
        );

        return;
      }

      if (rollen.contains('Hauswirtschaftskraft')) {
        Navigator.of(context).pushAndRemoveUntil(
          MaterialPageRoute(
            builder: (_) =>
            const HauswirtschaftPage(),
          ),
              (route) => false,
        );

        return;
      }

      setState(() {
        _fehler =
        'Für diese Benutzerrolle ist noch keine Oberfläche verfügbar.';
      });
    } catch (e) {
      if (!mounted) {
        return;
      }

      setState(() {
        _fehler =
        'Verbindung zum PSM-Server nicht möglich.';
      });
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(
                maxWidth: 420,
              ),
              child: Column(
                crossAxisAlignment:
                CrossAxisAlignment.stretch,
                children: [
                  const Icon(
                    Icons.health_and_safety,
                    size: 90,
                  ),

                  const SizedBox(height: 16),

                  const Text(
                    'PSM',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 38,
                      fontWeight: FontWeight.bold,
                    ),
                  ),

                  const SizedBox(height: 6),

                  const Text(
                    'Pflege Management System',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 16,
                    ),
                  ),

                  const SizedBox(height: 40),

                  TextField(
                    controller:
                    _benutzernameController,
                    enabled: !_isLoading,
                    textInputAction:
                    TextInputAction.next,
                    autofillHints: const [
                      AutofillHints.username,
                    ],
                    decoration:
                    const InputDecoration(
                      labelText: 'Benutzername',
                      prefixIcon:
                      Icon(Icons.person),
                      border:
                      OutlineInputBorder(),
                    ),
                  ),

                  const SizedBox(height: 16),

                  TextField(
                    controller:
                    _passwortController,
                    enabled: !_isLoading,
                    obscureText:
                    !_passwortAnzeigen,
                    textInputAction:
                    TextInputAction.done,
                    autofillHints: const [
                      AutofillHints.password,
                    ],
                    onSubmitted: (_) {
                      if (!_isLoading) {
                        _anmelden();
                      }
                    },
                    decoration: InputDecoration(
                      labelText: 'Passwort',
                      prefixIcon:
                      const Icon(Icons.lock),
                      border:
                      const OutlineInputBorder(),
                      suffixIcon: IconButton(
                        onPressed: () {
                          setState(() {
                            _passwortAnzeigen =
                            !_passwortAnzeigen;
                          });
                        },
                        icon: Icon(
                          _passwortAnzeigen
                              ? Icons.visibility_off
                              : Icons.visibility,
                        ),
                      ),
                    ),
                  ),

                  if (_fehler != null) ...[
                    const SizedBox(height: 16),

                    Container(
                      padding:
                      const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: Theme.of(context)
                            .colorScheme
                            .errorContainer,
                        borderRadius:
                        BorderRadius.circular(8),
                      ),
                      child: Text(
                        _fehler!,
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          color: Theme.of(context)
                              .colorScheme
                              .onErrorContainer,
                        ),
                      ),
                    ),
                  ],

                  const SizedBox(height: 24),

                  SizedBox(
                    height: 52,
                    child: FilledButton(
                      onPressed:
                      _isLoading
                          ? null
                          : _anmelden,
                      child: _isLoading
                          ? const SizedBox(
                        width: 24,
                        height: 24,
                        child:
                        CircularProgressIndicator(
                          strokeWidth: 2,
                        ),
                      )
                          : const Text(
                        'Anmelden',
                        style: TextStyle(
                          fontSize: 16,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}