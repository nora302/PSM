import 'package:flutter/material.dart';

import 'auth_service.dart';
import 'login_page.dart';

class HauswirtschaftPage extends StatelessWidget {
  const HauswirtschaftPage({super.key});

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
        title: const Text('PSM - Hauswirtschaft'),
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
              'Hauswirtschaft',
              style: TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.bold,
              ),
            ),

            const SizedBox(height: 8),

            const Text(
              'Pflege Management System',
              style: TextStyle(
                fontSize: 16,
              ),
            ),

            const SizedBox(height: 30),

            Card(
              child: ListTile(
                leading: const Icon(
                  Icons.cleaning_services,
                  size: 36,
                ),
                title: const Text(
                  'Hauswirtschaft',
                ),
                subtitle: const Text(
                  'Aufgaben und Tätigkeiten',
                ),
                trailing: const Icon(
                  Icons.arrow_forward_ios,
                ),
                onTap: () {
                  // Funktion wird später ergänzt.
                },
              ),
            ),

            const SizedBox(height: 12),

            Card(
              child: ListTile(
                leading: const Icon(
                  Icons.elderly,
                  size: 36,
                ),
                title: const Text('Bewohner'),
                subtitle: const Text(
                  'Bewohner des Standorts anzeigen',
                ),
                trailing: const Icon(
                  Icons.arrow_forward_ios,
                ),
                onTap: () {
                  // Wird später mit der API verbunden.
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}