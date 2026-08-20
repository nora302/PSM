import 'package:flutter/material.dart';

import 'pflegebericht_page.dart';

class BewohnerPage extends StatelessWidget {
  const BewohnerPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Bewohner'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            child: ListTile(
              leading: const CircleAvatar(
                child: Icon(Icons.person),
              ),
              title: const Text(
                'Hans Schneider',
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                ),
              ),
              subtitle: const Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SizedBox(height: 4),
                  Text('Zimmer 101'),
                  Text('Bewohnernummer: 1001'),
                ],
              ),
              trailing: const Icon(
                Icons.arrow_forward_ios,
              ),
              onTap: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => const PflegeberichtPage(
                      bewohnerId:
                      'f3fe3cc9-d6cf-40cc-9e87-bd8e6a2fcf11',
                      bewohnerName: 'Hans Schneider',
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}