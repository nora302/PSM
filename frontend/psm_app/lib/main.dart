import 'package:flutter/material.dart';
import 'login_page.dart';

void main() {
  runApp(const PsmApp());
}

class PsmApp extends StatelessWidget {
  const PsmApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'PSM',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
      ),
      home: const LoginPage(),
    );
  }
}