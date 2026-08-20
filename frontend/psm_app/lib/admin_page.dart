import 'package:flutter/material.dart';

import 'auth_service.dart';
import 'benutzer_page.dart';
import 'login_page.dart';

class AdminPage extends StatelessWidget {
  const AdminPage({super.key});

  void _abmelden(BuildContext context) {
    AuthService.logout();

    Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(
        builder: (_) => const LoginPage(),
      ),
          (route) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('PSM - Administration'),
        actions: [
          IconButton(
            tooltip: 'Abmelden',
            onPressed: () => _abmelden(context),
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'Administration',
              style: TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),

            const Text(
              'Pflege Management System verwalten',
              style: TextStyle(
                fontSize: 16,
              ),
            ),

            const SizedBox(height: 30),

            Card(
              child: ListTile(
                leading: const Icon(
                  Icons.manage_accounts,
                  size: 36,
                ),
                title: const Text(
                  'Benutzerverwaltung',
                ),
                subtitle: const Text(
                  'Mitarbeiter erstellen und verwalten',
                ),
                trailing: const Icon(
                  Icons.arrow_forward_ios,
                ),
                onTap: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) =>
                      const BenutzerPage(),
                    ),
                  );
                },
              ),
            ),

            const SizedBox(height: 12),

            const Card(
              child: ListTile(
                leading: Icon(
                  Icons.location_on,
                  size: 36,
                ),
                title: Text('Standorte'),
                subtitle: Text(
                  'Standorte verwalten',
                ),
                trailing: Icon(
                  Icons.arrow_forward_ios,
                ),
              ),
            ),

            const SizedBox(height: 12),

            const Card(
              child: ListTile(
                leading: Icon(
                  Icons.elderly,
                  size: 36,
                ),
                title: Text('Bewohner'),
                subtitle: Text(
                  'Bewohner verwalten',
                ),
                trailing: Icon(
                  Icons.arrow_forward_ios,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}