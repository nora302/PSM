import 'package:flutter/foundation.dart';

class ApiService {
  static String get baseUrl {
    if (kIsWeb) {
      return 'http://localhost:5233';
    }

    return 'http://10.0.2.2:5233';
  }
}