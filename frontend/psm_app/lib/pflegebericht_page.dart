import 'package:flutter/material.dart';

class PflegeberichtPage extends StatefulWidget {
  final String bewohnerId;
  final String bewohnerName;

  const PflegeberichtPage({
    super.key,
    required this.bewohnerId,
    required this.bewohnerName,
  });

  @override
  State<PflegeberichtPage> createState() =>
      _PflegeberichtPageState();
}

class _PflegeberichtPageState extends State<PflegeberichtPage> {
  final TextEditingController _berichtController =
  TextEditingController();

  String _schicht = 'Frühschicht';
  bool _spracheingabe = false;

  final List<String> _schichten = [
    'Frühschicht',
    'Spätdienst',
    'Nachtschicht',
  ];

  @override
  void dispose() {
    _berichtController.dispose();
    super.dispose();
  }

  void _spracheStarten() {
    setState(() {
      _spracheingabe = true;
    });

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'Speech-to-Text wird anschließend mit der API verbunden.',
        ),
      ),
    );
  }

  void _speichern() {
    if (_berichtController.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Bitte einen Pflegebericht eingeben.',
          ),
        ),
      );

      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'Speicherung über die API wird anschließend verbunden.',
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Pflegebericht'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: ConstrainedBox(
          constraints: const BoxConstraints(
            maxWidth: 700,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                widget.bewohnerName,
                style: const TextStyle(
                  fontSize: 26,
                  fontWeight: FontWeight.bold,
                ),
              ),

              const SizedBox(height: 6),

              const Text(
                'Pflegedokumentation',
                style: TextStyle(
                  fontSize: 16,
                ),
              ),

              const SizedBox(height: 24),

              DropdownButtonFormField<String>(
                initialValue: _schicht,
                decoration: const InputDecoration(
                  labelText: 'Schicht',
                  prefixIcon: Icon(Icons.schedule),
                  border: OutlineInputBorder(),
                ),
                items: _schichten.map((schicht) {
                  return DropdownMenuItem<String>(
                    value: schicht,
                    child: Text(schicht),
                  );
                }).toList(),
                onChanged: (value) {
                  if (value != null) {
                    setState(() {
                      _schicht = value;
                    });
                  }
                },
              ),

              const SizedBox(height: 20),

              TextField(
                controller: _berichtController,
                minLines: 7,
                maxLines: 12,
                decoration: const InputDecoration(
                  labelText: 'Pflegebericht',
                  hintText:
                  'Pflegebericht eingeben oder per Sprache erfassen...',
                  alignLabelWithHint: true,
                  border: OutlineInputBorder(),
                ),
              ),

              const SizedBox(height: 16),

              SizedBox(
                height: 52,
                child: OutlinedButton.icon(
                  onPressed: _spracheStarten,
                  icon: Icon(
                    _spracheingabe
                        ? Icons.mic
                        : Icons.mic_none,
                  ),
                  label: const Text(
                    'Spracheingabe',
                  ),
                ),
              ),

              const SizedBox(height: 12),

              SizedBox(
                height: 52,
                child: FilledButton.icon(
                  onPressed: _speichern,
                  icon: const Icon(Icons.save),
                  label: Text(
                    '$_schicht speichern',
                  ),
                ),
              ),

              const SizedBox(height: 24),

              const Divider(),

              const SizedBox(height: 12),

              const Text(
                'Tagesbericht',
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                ),
              ),

              const SizedBox(height: 8),

              const Text(
                'Frühschicht + Spätdienst + Nachtschicht werden '
                    'zu einem täglichen Pflegebericht zusammengeführt.',
              ),

              const SizedBox(height: 16),

              OutlinedButton.icon(
                onPressed: () {
                  // PDF-Endpunkt wird anschließend verbunden.
                },
                icon: const Icon(Icons.picture_as_pdf),
                label: const Text(
                  'Tagesbericht als PDF',
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}