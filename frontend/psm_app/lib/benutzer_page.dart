import 'package:flutter/material.dart';

class BenutzerPage extends StatefulWidget {
  const BenutzerPage({super.key});

  @override
  State<BenutzerPage> createState() => _BenutzerPageState();
}

class _BenutzerPageState extends State<BenutzerPage> {
  final _formKey = GlobalKey<FormState>();

  final _vornameController = TextEditingController();
  final _nachnameController = TextEditingController();
  final _benutzernameController = TextEditingController();
  final _passwortController = TextEditingController();
  final _standortController = TextEditingController(
    text: '1',
  );

  String _rolle = 'Pflegekraft';
  bool _passwortAnzeigen = false;

  final List<String> _rollen = [
    'Pflegekraft',
    'Hauswirtschaftskraft',
    'Kuechenmitarbeiter',
    'Administrator',
  ];

  @override
  void dispose() {
    _vornameController.dispose();
    _nachnameController.dispose();
    _benutzernameController.dispose();
    _passwortController.dispose();
    _standortController.dispose();
    super.dispose();
  }

  void _benutzerErstellen() {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'API-Verbindung wird im nächsten Schritt hinzugefügt.',
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Benutzerverwaltung'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: ConstrainedBox(
          constraints: const BoxConstraints(
            maxWidth: 600,
          ),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment:
              CrossAxisAlignment.stretch,
              children: [
                const Text(
                  'Neuen Benutzer erstellen',
                  style: TextStyle(
                    fontSize: 26,
                    fontWeight: FontWeight.bold,
                  ),
                ),

                const SizedBox(height: 24),

                TextFormField(
                  controller: _vornameController,
                  decoration: const InputDecoration(
                    labelText: 'Vorname',
                    prefixIcon: Icon(Icons.person),
                    border: OutlineInputBorder(),
                  ),
                  validator: (value) {
                    if (value == null ||
                        value.trim().isEmpty) {
                      return 'Vorname eingeben.';
                    }

                    return null;
                  },
                ),

                const SizedBox(height: 16),

                TextFormField(
                  controller: _nachnameController,
                  decoration: const InputDecoration(
                    labelText: 'Nachname',
                    prefixIcon:
                    Icon(Icons.person_outline),
                    border: OutlineInputBorder(),
                  ),
                  validator: (value) {
                    if (value == null ||
                        value.trim().isEmpty) {
                      return 'Nachname eingeben.';
                    }

                    return null;
                  },
                ),

                const SizedBox(height: 16),

                TextFormField(
                  controller: _benutzernameController,
                  decoration: const InputDecoration(
                    labelText: 'Benutzername',
                    prefixIcon:
                    Icon(Icons.account_circle),
                    border: OutlineInputBorder(),
                  ),
                  validator: (value) {
                    if (value == null ||
                        value.trim().isEmpty) {
                      return 'Benutzername eingeben.';
                    }

                    return null;
                  },
                ),

                const SizedBox(height: 16),

                TextFormField(
                  controller: _passwortController,
                  obscureText: !_passwortAnzeigen,
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
                  validator: (value) {
                    if (value == null ||
                        value.isEmpty) {
                      return 'Passwort eingeben.';
                    }

                    if (value.length < 4) {
                      return 'Mindestens 4 Zeichen.';
                    }

                    return null;
                  },
                ),

                const SizedBox(height: 16),

                DropdownButtonFormField<String>(
                  initialValue: _rolle,
                  decoration: const InputDecoration(
                    labelText: 'Rolle',
                    prefixIcon: Icon(Icons.badge),
                    border: OutlineInputBorder(),
                  ),
                  items: _rollen.map((rolle) {
                    return DropdownMenuItem<String>(
                      value: rolle,
                      child: Text(rolle),
                    );
                  }).toList(),
                  onChanged: (value) {
                    if (value != null) {
                      setState(() {
                        _rolle = value;
                      });
                    }
                  },
                ),

                const SizedBox(height: 16),

                TextFormField(
                  controller: _standortController,
                  keyboardType:
                  TextInputType.number,
                  decoration: const InputDecoration(
                    labelText: 'Standort ID',
                    prefixIcon:
                    Icon(Icons.location_on),
                    border: OutlineInputBorder(),
                  ),
                  validator: (value) {
                    if (_rolle ==
                        'Pflegekraft' ||
                        _rolle ==
                            'Hauswirtschaftskraft') {
                      if (value == null ||
                          value.trim().isEmpty) {
                        return 'Standort ID eingeben.';
                      }

                      if (int.tryParse(
                        value.trim(),
                      ) ==
                          null) {
                        return 'Ungültige Standort ID.';
                      }
                    }

                    return null;
                  },
                ),

                const SizedBox(height: 28),

                SizedBox(
                  height: 52,
                  child: FilledButton.icon(
                    onPressed:
                    _benutzerErstellen,
                    icon:
                    const Icon(Icons.person_add),
                    label: const Text(
                      'Benutzer erstellen',
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
    );
  }
}