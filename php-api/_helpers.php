<?php
require_once __DIR__ . '/../includes/db.php';
require_once __DIR__ . '/../includes/functions.php';

function api_send_json(array $data, int $status = 200): void {
    http_response_code($status);
    header('Content-Type: application/json');
    echo json_encode($data);
    exit;
}

function api_read_json_body(): ?array {
    $input = file_get_contents('php://input');
    if ($input === false || $input === '') {
        return null;
    }
    $data = json_decode($input, true);
    return is_array($data) ? $data : null;
}

function api_get_header(string $name): ?string {
    $headers = function_exists('getallheaders') ? getallheaders() : [];
    foreach ($headers as $key => $value) {
        if (strcasecmp($key, $name) === 0) {
            return is_array($value) ? ($value[0] ?? null) : $value;
        }
    }
    return null;
}

function api_base_url(): string {
    $isHttps = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off')
        || (isset($_SERVER['SERVER_PORT']) && (int)$_SERVER['SERVER_PORT'] === 443);
    $scheme = $isHttps ? 'https' : 'http';
    $host = $_SERVER['HTTP_HOST'] ?? 'localhost';
    return $scheme . '://' . $host;
}

function api_parse_date(string $value): ?string {
    $dt = DateTimeImmutable::createFromFormat('Y-m-d', $value);
    if (!$dt || $dt->format('Y-m-d') !== $value) {
        return null;
    }
    return $dt->format('Y-m-d');
}

function api_parse_datetime(string $value, DateTimeZone $tz): ?DateTimeImmutable {
    try {
        $dt = new DateTimeImmutable($value);
        return $dt->setTimezone($tz);
    } catch (Exception $e) {
        return null;
    }
}

function api_hash_template(string $rawBytes): string {
    return hash('sha256', $rawBytes);
}

function api_duration_hms(DateTimeImmutable $start, DateTimeImmutable $end): string {
    $seconds = max(0, $end->getTimestamp() - $start->getTimestamp());
    $hours = floor($seconds / 3600);
    $minutes = floor(($seconds % 3600) / 60);
    $secs = $seconds % 60;
    return sprintf('%02d:%02d:%02d', $hours, $minutes, $secs);
}

function api_require_method(string $method): void {
    if (strtoupper($_SERVER['REQUEST_METHOD'] ?? '') !== strtoupper($method)) {
        api_send_json(['success' => false, 'message' => 'Method not allowed'], 405);
    }
}

function api_validate_api_key(): void {
    $expected = $_ENV['API_KEY'] ?? '';
    $provided = api_get_header('X-API-Key');
    if ($expected !== '' && $provided !== $expected) {
        api_send_json(['success' => false, 'message' => 'Unauthorized'], 401);
    }
}

function api_student_by_reg_no(PDO $pdo, string $regNo): ?array {
    $sql = "
        SELECT s.*,
               p.name AS package_name,
               c.name AS class_name,
               lr.renewal_date,
               DATEDIFF(lr.renewal_date, CURDATE()) AS days_left
        FROM students s
        LEFT JOIN packages p ON s.package_id = p.id
        LEFT JOIN classes c ON s.class_id = c.id
        LEFT JOIN (
            SELECT reg_no, MAX(renewal_date) AS renewal_date
            FROM transactions
            WHERE status = 'completed'
            GROUP BY reg_no
        ) lr ON lr.reg_no = s.reg_no COLLATE utf8mb4_unicode_ci
        WHERE s.reg_no = ?
        LIMIT 1
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$regNo]);
    $student = $stmt->fetch(PDO::FETCH_ASSOC);
    return $student ?: null;
}

function api_load_student_relations(PDO $pdo, int $studentId): array {
    $relations = [];

    $stmt = $pdo->prepare("
        SELECT p.name
        FROM student_packages sp
        JOIN packages p ON sp.package_id = p.id
        WHERE sp.student_id = ?
    ");
    $stmt->execute([$studentId]);
    $relations['packages'] = $stmt->fetchAll(PDO::FETCH_COLUMN);

    $stmt = $pdo->prepare("
        SELECT c.name
        FROM student_classes sc
        JOIN classes c ON sc.class_id = c.id
        WHERE sc.student_id = ?
    ");
    $stmt->execute([$studentId]);
    $relations['classes'] = $stmt->fetchAll(PDO::FETCH_COLUMN);

    $stmt = $pdo->prepare("
        SELECT ac.name
        FROM student_art_courses sac
        JOIN art_courses ac ON sac.art_course_id = ac.id
        WHERE sac.student_id = ?
    ");
    $stmt->execute([$studentId]);
    $relations['art_courses'] = $stmt->fetchAll(PDO::FETCH_COLUMN);

    $stmt = $pdo->prepare("
        SELECT ns.name
        FROM student_nursing_schools sns
        JOIN nursing_schools ns ON sns.nursing_school_id = ns.id
        WHERE sns.student_id = ?
    ");
    $stmt->execute([$studentId]);
    $relations['nursing_schools'] = $stmt->fetchAll(PDO::FETCH_COLUMN);

    $stmt = $pdo->prepare("
        SELECT pr.name
        FROM student_programmes sp
        JOIN programmes pr ON sp.programme_id = pr.id
        WHERE sp.student_id = ?
    ");
    $stmt->execute([$studentId]);
    $relations['programmes'] = $stmt->fetchAll(PDO::FETCH_COLUMN);

    $stmt = $pdo->prepare("
        SELECT sc.name
        FROM student_science_courses ssc
        JOIN science_courses sc ON ssc.science_course_id = sc.id
        WHERE ssc.student_id = ?
    ");
    $stmt->execute([$studentId]);
    $relations['science_courses'] = $stmt->fetchAll(PDO::FETCH_COLUMN);

    $stmt = $pdo->prepare("
        SELECT sr.name
        FROM student_sources ss
        JOIN sources sr ON ss.source_id = sr.id
        WHERE ss.student_id = ?
    ");
    $stmt->execute([$studentId]);
    $relations['sources'] = $stmt->fetchAll(PDO::FETCH_COLUMN);

    $stmt = $pdo->prepare("
        SELECT su.name
        FROM student_subjects ssu
        JOIN subjects su ON ssu.subject_id = su.id
        WHERE ssu.student_id = ?
    ");
    $stmt->execute([$studentId]);
    $relations['subjects'] = $stmt->fetchAll(PDO::FETCH_COLUMN);

    $stmt = $pdo->prepare("
        SELECT u.name
        FROM student_universities su
        JOIN universities u ON su.university_id = u.id
        WHERE su.student_id = ?
    ");
    $stmt->execute([$studentId]);
    $relations['universities'] = $stmt->fetchAll(PDO::FETCH_COLUMN);

    return $relations;
}

function api_load_student_transactions(PDO $pdo, string $regNo): array {
    $stmt = $pdo->prepare("
        SELECT id, student_id, reg_no, package_id, subscription_months,
               discount_id, discount_amount, registration_fee, monthly_fee,
               subtotal, amount, gateway_charge, transaction_id, gateway_reference,
               gateway_slug, status, payment_date, renewal_date, created_at
        FROM transactions
        WHERE reg_no = ?
        ORDER BY payment_date DESC, id DESC
    ");
    $stmt->execute([$regNo]);
    $rows = $stmt->fetchAll(PDO::FETCH_ASSOC);
    $transactions = [];

    foreach ($rows as $row) {
        $transactions[] = [
            'id' => (int)$row['id'],
            'studentId' => (int)$row['student_id'],
            'regNo' => $row['reg_no'],
            'packageId' => $row['package_id'] !== null ? (int)$row['package_id'] : null,
            'subscriptionMonths' => $row['subscription_months'] !== null ? (int)$row['subscription_months'] : null,
            'discountId' => $row['discount_id'] !== null ? (int)$row['discount_id'] : null,
            'discountAmount' => $row['discount_amount'] !== null ? (string)$row['discount_amount'] : null,
            'registrationFee' => $row['registration_fee'] !== null ? (string)$row['registration_fee'] : null,
            'monthlyFee' => $row['monthly_fee'] !== null ? (string)$row['monthly_fee'] : null,
            'subtotal' => $row['subtotal'] !== null ? (string)$row['subtotal'] : null,
            'amount' => (string)$row['amount'],
            'gatewayCharge' => $row['gateway_charge'] !== null ? (string)$row['gateway_charge'] : null,
            'transactionId' => $row['transaction_id'],
            'gatewayReference' => $row['gateway_reference'],
            'gatewaySlug' => $row['gateway_slug'],
            'status' => $row['status'],
            'paymentDate' => $row['payment_date'],
            'renewalDate' => $row['renewal_date'],
            'createdAt' => $row['created_at'],
        ];
    }

    return $transactions;
}

/**
 * Validate base64 string
 */
function api_is_valid_base64(?string $data): bool {
    if ($data === null || $data === '') {
        return false;
    }
    $decoded = base64_decode($data, true);
    return $decoded !== false && $decoded !== '';
}

/**
 * Get fingerprint storage directory path
 */
function api_get_fingerprint_storage_path(): string {
    return __DIR__ . '/../storage/fingerprints';
}

/**
 * Save fingerprint preview image to storage
 */
function api_save_fingerprint_preview(string $filename, string $base64Data): bool {
    $storagePath = api_get_fingerprint_storage_path();

    if (!is_dir($storagePath)) {
        if (!mkdir($storagePath, 0755, true)) {
            return false;
        }
    }

    $imageData = base64_decode($base64Data, true);
    if ($imageData === false) {
        return false;
    }

    $filePath = $storagePath . '/' . basename($filename);
    return file_put_contents($filePath, $imageData) !== false;
}

/**
 * Generate fingerprint preview filename
 */
function api_generate_fingerprint_filename(string $regNo, int $fingerIndex): string {
    $safeRegNo = preg_replace('/[^a-zA-Z0-9]/', '_', $regNo);
    $timestamp = (new DateTimeImmutable('now', new DateTimeZone('Africa/Lagos')))->format('YmdHisu');
    return "fingerprint_{$safeRegNo}_{$fingerIndex}_{$timestamp}.png";
}

/**
 * Get finger name from finger index (1-10)
 */
function api_get_finger_name(int $index): ?string {
    $fingers = [
        1 => 'right-thumb',
        2 => 'right-index',
        3 => 'right-middle',
        4 => 'right-ring',
        5 => 'right-pinky',
        6 => 'left-thumb',
        7 => 'left-index',
        8 => 'left-middle',
        9 => 'left-ring',
        10 => 'left-pinky',
    ];
    return $fingers[$index] ?? null;
}

/**
 * Ensure fingerprint_enrollments table exists
 *
 * Schema:
 * - template: LONGBLOB - decoded binary fingerprint template (raw bytes)
 * - template_data: LONGTEXT - base64-encoded fingerprint template (for easy API responses)
 */
function api_ensure_fingerprint_enrollments_table(PDO $pdo): void {
    $pdo->exec("
        CREATE TABLE IF NOT EXISTS fingerprint_enrollments (
            id BIGINT AUTO_INCREMENT PRIMARY KEY,
            reg_no VARCHAR(50) NOT NULL,
            finger_index TINYINT NOT NULL,
            finger_name VARCHAR(50) DEFAULT NULL,
            template LONGBLOB NOT NULL,
            template_data LONGTEXT DEFAULT NULL,
            image_preview VARCHAR(255) DEFAULT NULL,
            captured_at DATETIME DEFAULT NULL,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
            UNIQUE KEY uq_regno_finger (reg_no, finger_index),
            INDEX idx_reg_no (reg_no)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    ");
}

/**
 * Upsert fingerprint enrollment record
 *
 * @param string $templateBytes Decoded binary template (raw bytes)
 * @param string|null $templateBase64 Base64-encoded template string
 */
function api_upsert_fingerprint_enrollment(
    PDO $pdo,
    string $regNo,
    int $fingerIndex,
    ?string $fingerName,
    string $templateBytes,
    ?string $templateBase64,
    ?string $imagePreview,
    ?string $capturedAt
): bool {
    $stmt = $pdo->prepare("
        INSERT INTO fingerprint_enrollments
        (reg_no, finger_index, finger_name, template, template_data, image_preview, captured_at)
        VALUES (?, ?, ?, ?, ?, ?, ?)
        ON DUPLICATE KEY UPDATE
        finger_name = VALUES(finger_name),
        template = VALUES(template),
        template_data = VALUES(template_data),
        image_preview = VALUES(image_preview),
        captured_at = VALUES(captured_at)
    ");

    return $stmt->execute([
        $regNo,
        $fingerIndex,
        $fingerName,
        $templateBytes,
        $templateBase64,
        $imagePreview,
        $capturedAt
    ]);
}

/**
 * Get fingerprint enrollments for a student
 *
 * Returns:
 * - template: base64-encoded (encoded from BLOB for response)
 * - template_data: base64 string (already stored as base64 in DB)
 */
function api_get_fingerprint_enrollments(PDO $pdo, string $regNo): array {
    $stmt = $pdo->prepare("
        SELECT reg_no, finger_index, finger_name, template, template_data, image_preview, captured_at
        FROM fingerprint_enrollments
        WHERE reg_no = ?
        ORDER BY finger_index ASC
    ");
    $stmt->execute([$regNo]);

    $records = [];
    while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
        $records[] = [
            'regno' => $row['reg_no'],
            'finger_index' => (int)$row['finger_index'],
            'finger_name' => $row['finger_name'],
            'template' => base64_encode($row['template']),
            'template_data' => $row['template_data'],
            'image_preview' => $row['image_preview'],
            'captured_at' => $row['captured_at'],
        ];
    }

    return $records;
}

/**
 * Ensure attendance_records table exists (new schema per spec)
 */
function api_ensure_attendance_records_table(PDO $pdo): void {
    $pdo->exec("
        CREATE TABLE IF NOT EXISTS attendance_records (
            id BIGINT AUTO_INCREMENT PRIMARY KEY,
            regno VARCHAR(50) NOT NULL,
            name VARCHAR(120) NOT NULL,
            class_name VARCHAR(120) DEFAULT NULL,
            date DATE NOT NULL,
            time_in DATETIME DEFAULT NULL,
            time_out DATETIME DEFAULT NULL,
            device_id VARCHAR(100) DEFAULT NULL,
            match_score INT DEFAULT NULL,
            match_far DOUBLE DEFAULT NULL,
            finger_index TINYINT DEFAULT NULL,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            INDEX idx_regno_date (regno, date),
            INDEX idx_date (date)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    ");
}

/**
 * Match fingerprint against enrolled templates via matcher microservice
 *
 * Calls the .NET matching microservice at BIO_MATCHER_URL.
 * Falls back to hash-based matching if service is not configured.
 *
 * Microservice request:
 * POST http://localhost:5085/match
 * { "templateBase64": "...", "regNo": "..." (optional) }
 *
 * Microservice response (match):
 * { "success": true, "regno": "...", "finger_index": 6, "match_score": 88, "far": 0.0002 }
 *
 * Microservice response (no match):
 * { "success": false, "message": "No match" }
 *
 * @param PDO $pdo Database connection
 * @param string $templateBase64 Base64-encoded fingerprint template
 * @param string|null $matcherServiceUrl URL of the matching microservice (BIO_MATCHER_URL)
 * @param string|null $regNoFilter Optional regNo to filter matching (for verification)
 * @return array|null Match result or null if no match
 */
function api_match_fingerprint(PDO $pdo, string $templateBase64, ?string $matcherServiceUrl = null, ?string $regNoFilter = null): ?array {
    $rawBytes = base64_decode($templateBase64, true);
    if ($rawBytes === false) {
        return null;
    }

    // Option A: Call external .NET matching microservice (BIO_MATCHER_URL)
    if ($matcherServiceUrl !== null && $matcherServiceUrl !== '') {
        $payload = ['templateBase64' => $templateBase64];
        if ($regNoFilter !== null && $regNoFilter !== '') {
            $payload['regNo'] = $regNoFilter;
        }

        $ch = curl_init($matcherServiceUrl);
        curl_setopt_array($ch, [
            CURLOPT_POST => true,
            CURLOPT_POSTFIELDS => json_encode($payload),
            CURLOPT_HTTPHEADER => ['Content-Type: application/json'],
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT => 10,
        ]);
        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);

        if ($httpCode === 200 && $response) {
            $result = json_decode($response, true);
            if (is_array($result) && isset($result['success']) && $result['success'] === true && !empty($result['regno'])) {
                // Fetch student details from DB to get name, class, passport
                $stmt = $pdo->prepare("
                    SELECT s.name, c.name AS class_name, s.passport_path
                    FROM students s
                    LEFT JOIN classes c ON s.class_id = c.id
                    WHERE s.reg_no = ? COLLATE utf8mb4_unicode_ci
                    LIMIT 1
                ");
                $stmt->execute([$result['regno']]);
                $student = $stmt->fetch(PDO::FETCH_ASSOC);

                return [
                    'regno' => $result['regno'],
                    'finger_index' => $result['finger_index'] ?? null,
                    'name' => $student['name'] ?? null,
                    'class_name' => $student['class_name'] ?? null,
                    'passport_path' => $student['passport_path'] ?? null,
                    'match_score' => $result['match_score'] ?? null,
                    'far' => $result['far'] ?? null,
                ];
            }
        }
        return null;
    }

    // Option B: Fallback to hash-based matching (for testing only)
    // In production, you should always use the microservice
    $hash = api_hash_template($rawBytes);

    // Try matching against fingerprint_enrollments table
    // Use COLLATE to handle collation mismatch between tables
    $sql = "
        SELECT fe.reg_no, fe.finger_index, s.name, c.name AS class_name, s.passport_path
        FROM fingerprint_enrollments fe
        JOIN students s ON fe.reg_no = s.reg_no COLLATE utf8mb4_unicode_ci
        LEFT JOIN classes c ON s.class_id = c.id
        WHERE SHA2(fe.template, 256) = ?
    ";
    $params = [$hash];
    if ($regNoFilter !== null && $regNoFilter !== '') {
        $sql .= " AND fe.reg_no = ?";
        $params[] = $regNoFilter;
    }
    $sql .= " LIMIT 1";

    $stmt = $pdo->prepare($sql);
    $stmt->execute($params);
    $row = $stmt->fetch(PDO::FETCH_ASSOC);

    if ($row) {
        return [
            'regno' => $row['reg_no'],
            'finger_index' => (int)$row['finger_index'],
            'name' => $row['name'],
            'class_name' => $row['class_name'],
            'passport_path' => $row['passport_path'],
            'match_score' => 100, // Hash match is exact
            'far' => 0.0,
        ];
    }

    // Also try the old fingerprint_enrollment table for backwards compatibility
    // Wrapped in try-catch as this table may not exist in newer installations
    try {
        $sql = "
            SELECT fe.reg_no, fe.finger_position, s.name, c.name AS class_name, s.passport_path
            FROM fingerprint_enrollment fe
            JOIN students s ON fe.reg_no = s.reg_no COLLATE utf8mb4_unicode_ci
            LEFT JOIN classes c ON s.class_id = c.id
            WHERE fe.fingerprint_hash = ? AND fe.is_active = 1
        ";
        $params = [$hash];
        if ($regNoFilter !== null && $regNoFilter !== '') {
            $sql .= " AND fe.reg_no = ?";
            $params[] = $regNoFilter;
        }
        $sql .= " LIMIT 1";

        $stmt = $pdo->prepare($sql);
        $stmt->execute($params);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);

        if ($row) {
            return [
                'regno' => $row['reg_no'],
                'finger_index' => null,
                'name' => $row['name'],
                'class_name' => $row['class_name'],
                'passport_path' => $row['passport_path'],
                'match_score' => 100,
                'far' => 0.0,
            ];
        }
    } catch (PDOException $e) {
        // Old table doesn't exist, that's fine
    }

    return null;
}

/**
 * Calculate duration in seconds between two timestamps
 */
function api_duration_seconds(DateTimeImmutable $start, DateTimeImmutable $end): int {
    return max(0, $end->getTimestamp() - $start->getTimestamp());
}

function api_student_to_response(array $student, array $relations): array {
    $baseUrl = api_base_url();
    $passport = $student['passport_path'] ?? null;
    $passportUrl = $passport ? $baseUrl . '/public/uploads/passports/' . ltrim($passport, '/') : null;

    return [
        'id' => (int)$student['id'],
        'regNo' => $student['reg_no'] ?? null,
        'matricNo' => $student['matric_no'] ?? null,
        'name' => $student['name'] ?? null,
        'fullName' => $student['name'] ?? null,
        'className' => $student['class_name'] ?? null,
        'class' => $student['class_name'] ?? null,
        'department' => $student['department'] ?? null,
        'faculty' => $student['faculty'] ?? null,
        'email' => $student['email'] ?? null,
        'gender' => $student['gender'] ?? null,
        'phone' => $student['phone'] ?? null,
        'guardianPhone' => $student['guardian_phone'] ?? null,
        'guardianEmail' => $student['guardian_email'] ?? null,
        'guardianName' => $student['guardian_name'] ?? null,
        'dob' => $student['dob'] ?? null,
        'address' => $student['address'] ?? null,
        'resumptionDate' => $student['resumption_date'] ?? null,
        'passport' => $passport,
        'passportUrl' => $passportUrl,
        'signature' => $student['signature'] ?? null,
        'subscriptionMonths' => isset($student['subscription_months']) ? (int)$student['subscription_months'] : null,
        'packageId' => isset($student['package_id']) ? (int)$student['package_id'] : null,
        'packageName' => $student['package_name'] ?? null,
        'classId' => isset($student['class_id']) ? (int)$student['class_id'] : null,
        'renewalDate' => $student['renewal_date'] ?? null,
        'daysLeft' => isset($student['days_left']) ? (int)$student['days_left'] : null,
        'createdAt' => $student['created_at'] ?? null,
        'packages' => $relations['packages'] ?? [],
        'classes' => $relations['classes'] ?? [],
        'artCourses' => $relations['art_courses'] ?? [],
        'nursingSchools' => $relations['nursing_schools'] ?? [],
        'programmes' => $relations['programmes'] ?? [],
        'scienceCourses' => $relations['science_courses'] ?? [],
        'sources' => $relations['sources'] ?? [],
        'subjects' => $relations['subjects'] ?? [],
        'universities' => $relations['universities'] ?? [],
    ];
}
