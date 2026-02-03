<?php
require_once __DIR__ . '/_helpers.php';

$dotenv = Dotenv\Dotenv::createImmutable(__DIR__ . '/..');
$dotenv->load();

$timezone = $_ENV['TIMEZONE'] ?? 'Africa/Lagos';
date_default_timezone_set($timezone);
$tz = new DateTimeZone($timezone);

$path = trim($_GET['path'] ?? '', '/');
$method = strtoupper($_SERVER['REQUEST_METHOD'] ?? 'GET');

api_validate_api_key();

try {
    if ($path === 'health' && $method === 'GET') {
        api_send_json([
            'status' => 'ok',
            'time' => (new DateTimeImmutable('now', $tz))->format(DateTimeInterface::ATOM),
            'version' => '1.0.0',
        ]);
    }

    if ($path === 'students' && $method === 'GET') {
        $regNo = clean_input($_GET['regNo'] ?? '');
        if ($regNo === '') {
            api_send_json(['success' => false, 'message' => 'Invalid regNo'], 400);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        $student = api_student_by_reg_no($pdo, $regNo);
        if (!$student) {
            api_send_json(['success' => false, 'message' => 'Student not found'], 404);
        }

        $relations = api_load_student_relations($pdo, (int)$student['id']);
        $payload = api_student_to_response($student, $relations);
        $payload['transactions'] = api_load_student_transactions($pdo, $regNo);

        api_send_json(['success' => true, 'student' => $payload]);
    }

    if ($path === 'students/photo' && $method === 'GET') {
        $regNo = clean_input($_GET['regNo'] ?? '');
        if ($regNo === '') {
            api_send_json(['success' => false, 'message' => 'Invalid regNo'], 400);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        $stmt = $pdo->prepare("SELECT passport_path FROM students WHERE reg_no = ? LIMIT 1");
        $stmt->execute([$regNo]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);
        if (!$row || empty($row['passport_path'])) {
            api_send_json(['success' => false, 'message' => 'Photo not found'], 404);
        }

        $filePath = __DIR__ . '/../uploads/' . basename($row['passport_path']);
        if (!is_file($filePath)) {
            api_send_json(['success' => false, 'message' => 'Photo not found'], 404);
        }

        $mime = mime_content_type($filePath) ?: 'application/octet-stream';
        header('Content-Type: ' . $mime);
        readfile($filePath);
        exit;
    }

    if ($path === 'enrollment/status' && $method === 'GET') {
        $regNo = clean_input($_GET['regNo'] ?? '');
        if ($regNo === '') {
            api_send_json(['success' => false, 'message' => 'Invalid regNo'], 400);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        $stmt = $pdo->prepare("SELECT id FROM students WHERE reg_no = ? LIMIT 1");
        $stmt->execute([$regNo]);
        $student = $stmt->fetch(PDO::FETCH_ASSOC);
        if (!$student) {
            api_send_json(['success' => false, 'message' => 'Student not found'], 404);
        }

        $stmt = $pdo->prepare("
            SELECT COUNT(*) AS cnt, MIN(enrolled_at) AS enrolled_at
            FROM fingerprint_enrollment
            WHERE reg_no = ? AND is_active = 1
        ");
        $stmt->execute([$regNo]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);
        $count = (int)($row['cnt'] ?? 0);

        api_send_json([
            'success' => true,
            'regNo' => $regNo,
            'isEnrolled' => $count > 0,
            'fingerCount' => $count,
            'enrolledAt' => $count > 0 ? $row['enrolled_at'] : null,
        ]);
    }

    if ($path === 'enrollment/submit' && $method === 'POST') {
        $data = api_read_json_body();
        if (!$data) {
            api_send_json(['success' => false, 'message' => 'Invalid JSON payload'], 400);
        }

        $regNo = clean_input($data['regNo'] ?? '');
        $name = clean_input($data['name'] ?? '');
        $className = clean_input($data['className'] ?? '');
        $templates = $data['templates'] ?? null;
        $enrolledAtRaw = $data['enrolledAt'] ?? null;
        $deviceId = clean_input($data['deviceId'] ?? '');

        if ($regNo === '' || $name === '' || $className === '') {
            api_send_json(['success' => false, 'message' => 'Missing required fields'], 400);
        }
        if (!is_array($templates) || count($templates) === 0) {
            api_send_json(['success' => false, 'message' => 'Templates are required'], 400);
        }
        if (count($templates) > 10) {
            api_send_json(['success' => false, 'message' => 'Too many templates'], 400);
        }

        $enrolledAt = null;
        if (is_string($enrolledAtRaw)) {
            $enrolledAt = api_parse_datetime($enrolledAtRaw, $tz);
        }
        if (!$enrolledAt) {
            api_send_json(['success' => false, 'message' => 'Invalid enrolledAt'], 422);
        }

        $allowedFingers = [
            'left_thumb','left_index','left_middle','left_ring','left_pinky',
            'right_thumb','right_index','right_middle','right_ring','right_pinky'
        ];

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        $stmt = $pdo->prepare("SELECT id, reg_no FROM students WHERE reg_no = ? LIMIT 1");
        $stmt->execute([$regNo]);
        $student = $stmt->fetch(PDO::FETCH_ASSOC);
        if (!$student) {
            api_send_json(['success' => false, 'message' => 'Student not found'], 404);
        }
        $studentId = (int)$student['id'];

        $stmt = $pdo->prepare("SELECT COUNT(*) FROM fingerprint_enrollment WHERE reg_no = ? AND is_active = 1");
        $stmt->execute([$regNo]);
        $currentCount = (int)$stmt->fetchColumn();
        if ($currentCount + count($templates) > 10) {
            api_send_json(['success' => false, 'message' => 'Fingerprint limit exceeded'], 409);
        }

        $seenFingers = [];
        $pdo->beginTransaction();
        try {
            foreach ($templates as $template) {
                $finger = clean_input($template['finger'] ?? '');
                $fingerIndex = $template['fingerIndex'] ?? null;
                $templateBase64 = $template['templateBase64'] ?? null;

                if (!in_array($finger, $allowedFingers, true)) {
                    throw new RuntimeException('Invalid finger value');
                }
                if (!is_int($fingerIndex) || $fingerIndex < 0 || $fingerIndex > 9) {
                    throw new RuntimeException('Invalid fingerIndex');
                }
                if (!is_string($templateBase64) || $templateBase64 === '') {
                    throw new RuntimeException('Missing templateBase64');
                }
                if (isset($seenFingers[$finger])) {
                    throw new RuntimeException('Duplicate finger in request');
                }
                $seenFingers[$finger] = true;

                $rawBytes = api_decode_template_base64($templateBase64);
                if ($rawBytes === null) {
                    throw new RuntimeException('Invalid templateBase64');
                }

                $stmt = $pdo->prepare("
                    SELECT 1 FROM fingerprint_enrollment
                    WHERE reg_no = ? AND finger_position = ? AND is_active = 1
                    LIMIT 1
                ");
                $stmt->execute([$regNo, $finger]);
                if ($stmt->fetchColumn()) {
                    throw new RuntimeException('Finger already enrolled');
                }

                $hash = api_hash_template($rawBytes);
                $insert = $pdo->prepare("
                    INSERT INTO fingerprint_enrollment
                    (reg_no, student_id, fingerprint_template, fingerprint_hash,
                     finger_position, device_id, enrolled_by, enrolled_at)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                ");
                $insert->execute([
                    $regNo,
                    $studentId,
                    $rawBytes,
                    $hash,
                    $finger,
                    $deviceId !== '' ? $deviceId : null,
                    api_get_header('X-API-Key') ?: 'api',
                    $enrolledAt->format('Y-m-d H:i:s')
                ]);
            }
            $pdo->commit();
        } catch (Exception $e) {
            $pdo->rollBack();
            $message = $e->getMessage();
            $status = 422;
            if ($message === 'Finger already enrolled' || $message === 'Duplicate finger in request' || $message === 'Fingerprint limit exceeded') {
                $status = 409;
            } elseif ($message === 'Invalid finger value' || $message === 'Invalid fingerIndex' || $message === 'Missing templateBase64' || $message === 'Invalid templateBase64') {
                $status = 422;
            }
            api_send_json(['success' => false, 'message' => $message], $status);
        }

        $stmt = $pdo->prepare("SELECT COUNT(*) FROM fingerprint_enrollment WHERE reg_no = ? AND is_active = 1");
        $stmt->execute([$regNo]);
        $fingerCount = (int)$stmt->fetchColumn();

        api_send_json([
            'success' => true,
            'message' => 'Enrollment saved',
            'regNo' => $regNo,
            'fingerCount' => $fingerCount,
            'enrolledAt' => $enrolledAt->format(DateTimeInterface::ATOM),
        ], 201);
    }

    // POST /api/attendance/clockin - Clock in a student by fingerprint
    if ($path === 'attendance/clockin' && $method === 'POST') {
        $data = api_read_json_body();
        if (!$data) {
            api_send_json(['success' => false, 'message' => 'Invalid JSON payload'], 400);
        }

        $templateBase64 = $data['templateBase64'] ?? null;
        $timestampRaw = $data['timestamp'] ?? null;
        $deviceId = clean_input($data['deviceId'] ?? '');

        if (!is_string($templateBase64) || $templateBase64 === '') {
            api_send_json(['success' => false, 'message' => 'templateBase64 is required'], 422);
        }
        if (!api_is_valid_base64($templateBase64)) {
            api_send_json(['success' => false, 'message' => 'Invalid templateBase64'], 422);
        }

        $dt = is_string($timestampRaw) ? api_parse_datetime($timestampRaw, $tz) : null;
        if (!$dt) {
            $dt = new DateTimeImmutable('now', $tz);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        // Ensure attendance_records table exists
        api_ensure_attendance_records_table($pdo);

        // Match fingerprint (uses microservice if configured, falls back to hash matching)
        $matcherServiceUrl = $_ENV['BIO_MATCHER_URL'] ?? null;
        $match = api_match_fingerprint($pdo, $templateBase64, $matcherServiceUrl);

        if (!$match) {
            api_send_json(['success' => false, 'message' => 'No match'], 404);
        }

        $regNo = $match['regno'];
        $studentName = $match['name'];
        $className = $match['class_name'];
        $passportPath = $match['passport_path'] ?? null;
        $fingerIndex = $match['finger_index'] ?? null;
        $matchScore = $match['match_score'] ?? null;
        $matchFar = $match['far'] ?? null;

        $attendanceDate = $dt->format('Y-m-d');

        // Check if already clocked in today
        $stmt = $pdo->prepare("
            SELECT id, time_in, time_out
            FROM attendance_records
            WHERE regno = ? AND date = ?
            ORDER BY time_in DESC
            LIMIT 1
        ");
        $stmt->execute([$regNo, $attendanceDate]);
        $existing = $stmt->fetch(PDO::FETCH_ASSOC);

        $passportUrl = $passportPath
            ? api_base_url() . '/uploads/' . ltrim($passportPath, '/')
            : null;

        if ($existing && $existing['time_in']) {
            $clockInTime = $existing['time_in'];
            api_send_json([
                'success' => true,
                'message' => 'Already clocked in',
                'student' => [
                    'regNo' => $regNo,
                    'name' => $studentName,
                    'className' => $className,
                    'passportUrl' => $passportUrl,
                ],
                'clockInTime' => $clockInTime,
                'alreadyClockedIn' => true,
            ]);
        }

        // Insert new attendance record
        $clockInTime = $dt->format('Y-m-d H:i:s');
        $insert = $pdo->prepare("
            INSERT INTO attendance_records
            (regno, name, class_name, date, time_in, device_id, match_score, match_far, finger_index, created_at)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        ");
        $insert->execute([
            $regNo,
            $studentName,
            $className,
            $attendanceDate,
            $clockInTime,
            $deviceId !== '' ? $deviceId : null,
            $matchScore,
            $matchFar,
            $fingerIndex,
            $dt->format('Y-m-d H:i:s'),
        ]);

        api_send_json([
            'success' => true,
            'message' => 'Clock-in successful',
            'student' => [
                'regNo' => $regNo,
                'name' => $studentName,
                'className' => $className,
                'passportUrl' => $passportUrl,
            ],
            'clockInTime' => $clockInTime,
            'alreadyClockedIn' => false,
        ]);
    }

    // POST /api/attendance/clockout - Clock out a student by fingerprint
    if ($path === 'attendance/clockout' && $method === 'POST') {
        $data = api_read_json_body();
        if (!$data) {
            api_send_json(['success' => false, 'message' => 'Invalid JSON payload'], 400);
        }

        $templateBase64 = $data['templateBase64'] ?? null;
        $timestampRaw = $data['timestamp'] ?? null;
        $deviceId = clean_input($data['deviceId'] ?? '');

        if (!is_string($templateBase64) || $templateBase64 === '') {
            api_send_json(['success' => false, 'message' => 'templateBase64 is required'], 422);
        }
        if (!api_is_valid_base64($templateBase64)) {
            api_send_json(['success' => false, 'message' => 'Invalid templateBase64'], 422);
        }

        $dt = is_string($timestampRaw) ? api_parse_datetime($timestampRaw, $tz) : null;
        if (!$dt) {
            $dt = new DateTimeImmutable('now', $tz);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        // Ensure attendance_records table exists
        api_ensure_attendance_records_table($pdo);

        // Match fingerprint
        $matcherServiceUrl = $_ENV['BIO_MATCHER_URL'] ?? null;
        $match = api_match_fingerprint($pdo, $templateBase64, $matcherServiceUrl);

        if (!$match) {
            api_send_json(['success' => false, 'message' => 'No match'], 404);
        }

        $regNo = $match['regno'];
        $studentName = $match['name'];
        $className = $match['class_name'];

        $attendanceDate = $dt->format('Y-m-d');

        // Find open attendance record (clocked in but not out)
        $stmt = $pdo->prepare("
            SELECT id, time_in, time_out
            FROM attendance_records
            WHERE regno = ? AND date = ? AND time_out IS NULL
            ORDER BY time_in DESC
            LIMIT 1
        ");
        $stmt->execute([$regNo, $attendanceDate]);
        $record = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$record) {
            api_send_json([
                'success' => true,
                'message' => 'Not clocked in',
                'student' => [
                    'regNo' => $regNo,
                    'name' => $studentName,
                    'className' => $className,
                ],
                'clockInTime' => null,
                'clockOutTime' => null,
                'duration' => null,
                'notClockedIn' => true,
            ]);
        }

        // Update with clock out time
        $clockOutTime = $dt->format('Y-m-d H:i:s');
        $update = $pdo->prepare("
            UPDATE attendance_records
            SET time_out = ?, device_id = COALESCE(?, device_id)
            WHERE id = ?
        ");
        $update->execute([
            $clockOutTime,
            $deviceId !== '' ? $deviceId : null,
            (int)$record['id'],
        ]);

        $clockIn = new DateTimeImmutable($record['time_in'], $tz);
        $clockOut = $dt;
        $duration = api_duration_seconds($clockIn, $clockOut);

        api_send_json([
            'success' => true,
            'message' => 'Clock-out successful',
            'student' => [
                'regNo' => $regNo,
                'name' => $studentName,
                'className' => $className,
            ],
            'clockInTime' => $record['time_in'],
            'clockOutTime' => $clockOutTime,
            'duration' => $duration,
            'notClockedIn' => false,
        ]);
    }

    // POST /api/attendance/clockin-verified - Clock in with pre-verified fingerprint (client-side matching)
    if ($path === 'attendance/clockin-verified' && $method === 'POST') {
        $data = api_read_json_body();
        if (!$data) {
            api_send_json(['success' => false, 'message' => 'Invalid JSON payload'], 400);
        }

        $regNo = clean_input($data['regNo'] ?? $data['regno'] ?? '');
        $fingerIndex = $data['fingerIndex'] ?? $data['finger_index'] ?? null;
        $matchScore = $data['matchScore'] ?? $data['match_score'] ?? null;
        $matchFar = $data['matchFar'] ?? $data['match_far'] ?? null;
        $timestampRaw = $data['timestamp'] ?? null;
        $deviceId = clean_input($data['deviceId'] ?? $data['device_id'] ?? '');

        if ($regNo === '') {
            api_send_json(['success' => false, 'message' => 'regNo is required'], 422);
        }

        $dt = is_string($timestampRaw) ? api_parse_datetime($timestampRaw, $tz) : null;
        if (!$dt) {
            $dt = new DateTimeImmutable('now', $tz);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        // Verify student exists and get details
        $stmt = $pdo->prepare("
            SELECT s.name, c.name AS class_name, s.passport_path
            FROM students s
            LEFT JOIN classes c ON s.class_id = c.id
            WHERE s.reg_no = ?
            LIMIT 1
        ");
        $stmt->execute([$regNo]);
        $student = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$student) {
            api_send_json(['success' => false, 'message' => 'Student not found'], 404);
        }

        $studentName = $student['name'];
        $className = $student['class_name'];
        $passportPath = $student['passport_path'];

        // Ensure attendance_records table exists
        api_ensure_attendance_records_table($pdo);

        $attendanceDate = $dt->format('Y-m-d');

        // Check if already clocked in today
        $stmt = $pdo->prepare("
            SELECT id, time_in, time_out
            FROM attendance_records
            WHERE regno = ? AND date = ?
            ORDER BY time_in DESC
            LIMIT 1
        ");
        $stmt->execute([$regNo, $attendanceDate]);
        $existing = $stmt->fetch(PDO::FETCH_ASSOC);

        $passportUrl = $passportPath
            ? api_base_url() . '/uploads/' . ltrim($passportPath, '/')
            : null;

        if ($existing && $existing['time_in']) {
            $clockInTime = $existing['time_in'];
            api_send_json([
                'success' => true,
                'message' => 'Already clocked in',
                'student' => [
                    'regNo' => $regNo,
                    'name' => $studentName,
                    'className' => $className,
                    'passportUrl' => $passportUrl,
                ],
                'clockInTime' => $clockInTime,
                'alreadyClockedIn' => true,
            ]);
        }

        // Insert new attendance record
        $clockInTime = $dt->format('Y-m-d H:i:s');
        $insert = $pdo->prepare("
            INSERT INTO attendance_records
            (regno, name, class_name, date, time_in, device_id, match_score, match_far, finger_index, created_at)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        ");
        $insert->execute([
            $regNo,
            $studentName,
            $className,
            $attendanceDate,
            $clockInTime,
            $deviceId !== '' ? $deviceId : null,
            $matchScore,
            $matchFar,
            $fingerIndex,
            $dt->format('Y-m-d H:i:s'),
        ]);

        api_send_json([
            'success' => true,
            'message' => 'Clock-in successful',
            'student' => [
                'regNo' => $regNo,
                'name' => $studentName,
                'className' => $className,
                'passportUrl' => $passportUrl,
            ],
            'clockInTime' => $clockInTime,
            'alreadyClockedIn' => false,
        ]);
    }

    // POST /api/attendance/clockout-verified - Clock out with pre-verified fingerprint (client-side matching)
    if ($path === 'attendance/clockout-verified' && $method === 'POST') {
        $data = api_read_json_body();
        if (!$data) {
            api_send_json(['success' => false, 'message' => 'Invalid JSON payload'], 400);
        }

        $regNo = clean_input($data['regNo'] ?? $data['regno'] ?? '');
        $fingerIndex = $data['fingerIndex'] ?? $data['finger_index'] ?? null;
        $matchScore = $data['matchScore'] ?? $data['match_score'] ?? null;
        $timestampRaw = $data['timestamp'] ?? null;
        $deviceId = clean_input($data['deviceId'] ?? $data['device_id'] ?? '');

        if ($regNo === '') {
            api_send_json(['success' => false, 'message' => 'regNo is required'], 422);
        }

        $dt = is_string($timestampRaw) ? api_parse_datetime($timestampRaw, $tz) : null;
        if (!$dt) {
            $dt = new DateTimeImmutable('now', $tz);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        // Verify student exists and get details
        $stmt = $pdo->prepare("
            SELECT s.name, c.name AS class_name
            FROM students s
            LEFT JOIN classes c ON s.class_id = c.id
            WHERE s.reg_no = ?
            LIMIT 1
        ");
        $stmt->execute([$regNo]);
        $student = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$student) {
            api_send_json(['success' => false, 'message' => 'Student not found'], 404);
        }

        $studentName = $student['name'];
        $className = $student['class_name'];

        // Ensure attendance_records table exists
        api_ensure_attendance_records_table($pdo);

        $attendanceDate = $dt->format('Y-m-d');

        // Find open attendance record (clocked in but not out)
        $stmt = $pdo->prepare("
            SELECT id, time_in, time_out
            FROM attendance_records
            WHERE regno = ? AND date = ? AND time_out IS NULL
            ORDER BY time_in DESC
            LIMIT 1
        ");
        $stmt->execute([$regNo, $attendanceDate]);
        $record = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$record) {
            api_send_json([
                'success' => true,
                'message' => 'Not clocked in',
                'student' => [
                    'regNo' => $regNo,
                    'name' => $studentName,
                    'className' => $className,
                ],
                'clockInTime' => null,
                'clockOutTime' => null,
                'duration' => null,
                'notClockedIn' => true,
            ]);
        }

        // Update with clock out time
        $clockOutTime = $dt->format('Y-m-d H:i:s');
        $update = $pdo->prepare("
            UPDATE attendance_records
            SET time_out = ?, device_id = COALESCE(?, device_id)
            WHERE id = ?
        ");
        $update->execute([
            $clockOutTime,
            $deviceId !== '' ? $deviceId : null,
            (int)$record['id'],
        ]);

        $clockIn = new DateTimeImmutable($record['time_in'], $tz);
        $clockOut = $dt;
        $duration = api_duration_seconds($clockIn, $clockOut);

        api_send_json([
            'success' => true,
            'message' => 'Clock-out successful',
            'student' => [
                'regNo' => $regNo,
                'name' => $studentName,
                'className' => $className,
            ],
            'clockInTime' => $record['time_in'],
            'clockOutTime' => $clockOutTime,
            'duration' => $duration,
            'notClockedIn' => false,
        ]);
    }

    // GET /api/attendance - Get attendance records for a date range
    if ($path === 'attendance' && $method === 'GET') {
        $fromRaw = $_GET['from'] ?? '';
        $toRaw = $_GET['to'] ?? '';
        $regNo = clean_input($_GET['regno'] ?? '');

        $from = is_string($fromRaw) ? api_parse_date($fromRaw) : null;
        $to = is_string($toRaw) ? api_parse_date($toRaw) : null;
        if (!$from || !$to) {
            api_send_json(['success' => false, 'message' => 'Invalid from/to date'], 400);
        }

        $fromDt = new DateTimeImmutable($from, $tz);
        $toDt = new DateTimeImmutable($to, $tz);
        if ($fromDt > $toDt) {
            api_send_json(['success' => false, 'message' => 'from date must be before to date'], 400);
        }
        $days = (int)$fromDt->diff($toDt)->format('%a');
        if ($days > 31) {
            api_send_json(['success' => false, 'message' => 'Date range too large'], 422);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        // Ensure attendance_records table exists
        api_ensure_attendance_records_table($pdo);

        $params = [$from, $to];
        $where = "date BETWEEN ? AND ?";
        if ($regNo !== '') {
            $where .= " AND regno = ?";
            $params[] = $regNo;
        }

        $stmt = $pdo->prepare("
            SELECT id, regno, name, class_name, date, time_in, time_out
            FROM attendance_records
            WHERE {$where}
            ORDER BY date DESC, time_in DESC
        ");
        $stmt->execute($params);
        $rows = $stmt->fetchAll(PDO::FETCH_ASSOC);

        $items = [];
        foreach ($rows as $row) {
            $items[] = [
                'id' => (int)$row['id'],
                'regNo' => $row['regno'],
                'name' => $row['name'],
                'className' => $row['class_name'],
                'date' => $row['date'],
                'timeIn' => $row['time_in'],
                'timeOut' => $row['time_out'],
            ];
        }

        api_send_json($items);
    }

    // POST /api/enrollments - Save multiple fingerprint templates (batch insert)
    if ($path === 'enrollments' && $method === 'POST') {
        $data = api_read_json_body();
        if (!$data) {
            api_send_json(['success' => false, 'message' => 'Invalid JSON payload'], 400);
        }

        // Accept both 'regno' and 'regNo' (camelCase from C# client)
        $regNo = clean_input($data['regno'] ?? $data['regNo'] ?? '');
        $records = $data['records'] ?? null;

        if ($regNo === '') {
            api_send_json(['success' => false, 'message' => 'Missing regno'], 400);
        }
        if (!is_array($records) || count($records) === 0) {
            api_send_json(['success' => false, 'message' => 'Missing or empty records'], 400);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        // Ensure table exists
        api_ensure_fingerprint_enrollments_table($pdo);

        $savedCount = 0;
        $pdo->beginTransaction();

        try {
            foreach ($records as $record) {
                // Accept both snake_case and camelCase field names (C# client sends camelCase)
                $fingerIndex = $record['finger_index'] ?? $record['fingerIndex'] ?? null;
                $fingerName = clean_input($record['finger_name'] ?? $record['fingerName'] ?? '');
                $template = $record['template'] ?? $record['templateBase64'] ?? null;
                $templateData = $record['template_data'] ?? $record['templateData'] ?? null;
                $expectedByteCount = $record['templateBytes'] ?? $record['template_bytes'] ?? null;
                $imagePreview = clean_input($record['image_preview'] ?? $record['imagePreview'] ?? '');
                $imagePreviewData = $record['image_preview_data'] ?? $record['imagePreviewData'] ?? null;
                $capturedAtRaw = $record['captured_at'] ?? $record['capturedAt'] ?? null;

                // Validate finger_index (1-10)
                if (!is_int($fingerIndex) && !is_numeric($fingerIndex)) {
                    throw new RuntimeException('Invalid finger_index value');
                }
                $fingerIndex = (int)$fingerIndex;
                if ($fingerIndex < 1 || $fingerIndex > 10) {
                    throw new RuntimeException('finger_index must be between 1 and 10');
                }

                // Validate template (must be valid base64)
                if (empty($template) && empty($templateData)) {
                    throw new RuntimeException('template or template_data is required');
                }

                $templateBase64 = $template ?: $templateData;
                if (!api_is_valid_base64($templateBase64)) {
                    throw new RuntimeException('Invalid base64 template');
                }
                $decodedTemplate = api_decode_template_base64($templateBase64);
                if ($decodedTemplate === null) {
                    throw new RuntimeException('Invalid base64 template');
                }

                // Validate decoded byte count if provided
                if ($expectedByteCount !== null && is_numeric($expectedByteCount)) {
                    $actualByteCount = strlen($decodedTemplate);
                    if ($actualByteCount !== (int)$expectedByteCount) {
                        throw new RuntimeException("Template byte count mismatch: expected {$expectedByteCount}, got {$actualByteCount}");
                    }
                }

                // Get base64 string for template_data column (stored as TEXT, not decoded)
                $templateBase64ForStorage = $templateBase64;
                $templateHash = api_hash_template($decodedTemplate);

                // Use finger_name from request or generate from index
                if ($fingerName === '') {
                    $fingerName = api_get_finger_name($fingerIndex);
                }

                // Handle captured_at
                $capturedAt = null;
                if (!empty($capturedAtRaw) && is_string($capturedAtRaw)) {
                    $dt = api_parse_datetime($capturedAtRaw, $tz);
                    if ($dt) {
                        $capturedAt = $dt->format('Y-m-d H:i:s');
                    }
                }
                if ($capturedAt === null) {
                    $capturedAt = (new DateTimeImmutable('now', $tz))->format('Y-m-d H:i:s');
                }

                // Handle image preview
                $previewFilename = $imagePreview;
                if (!empty($imagePreviewData) && is_string($imagePreviewData)) {
                    if (!api_is_valid_base64($imagePreviewData)) {
                        throw new RuntimeException('Invalid base64 image_preview_data');
                    }
                    if ($previewFilename === '') {
                        $previewFilename = api_generate_fingerprint_filename($regNo, $fingerIndex);
                    }
                    if (!api_save_fingerprint_preview($previewFilename, $imagePreviewData)) {
                        log_error("Failed to save fingerprint preview: {$previewFilename}", 'api');
                    }
                }

                // Upsert the record
                // - decodedTemplate: binary BLOB (raw bytes)
                // - templateBase64ForStorage: base64 string (stored as TEXT)
                $success = api_upsert_fingerprint_enrollment(
                    $pdo,
                    $regNo,
                    $fingerIndex,
                    $fingerName,
                    $decodedTemplate,
                    $templateBase64ForStorage,
                    $templateHash,
                    $previewFilename !== '' ? $previewFilename : null,
                    $capturedAt
                );

                if ($success) {
                    $savedCount++;
                }
            }

            $pdo->commit();
        } catch (Exception $e) {
            $pdo->rollBack();
            $message = $e->getMessage();
            $status = 422;
            if (strpos($message, 'finger_index') !== false || strpos($message, 'Invalid') !== false) {
                $status = 422;
            }
            api_send_json(['success' => false, 'message' => $message], $status);
        }

        api_send_json([
            'success' => true,
            'message' => 'Enrollment saved',
            'saved' => $savedCount
        ], 201);
    }

    // GET /api/enrollments/templates/all - Get all templates for all students (for sync)
    if ($path === 'enrollments/templates/all' && $method === 'GET') {
        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        // Ensure table exists
        api_ensure_fingerprint_enrollments_table($pdo);

        $records = api_get_all_fingerprint_enrollments($pdo);

        api_send_json([
            'success' => true,
            'records' => $records
        ]);
    }

    // GET /api/enrollments/templates?regno=... - Get templates for a student
    if ($path === 'enrollments/templates' && $method === 'GET') {
        // Accept both 'regno' and 'regNo' (camelCase from C# client)
        $regNo = clean_input($_GET['regno'] ?? $_GET['regNo'] ?? '');
        if ($regNo === '') {
            api_send_json(['success' => false, 'message' => 'Missing regno parameter'], 400);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        // Ensure table exists
        api_ensure_fingerprint_enrollments_table($pdo);

        $records = api_get_fingerprint_enrollments($pdo, $regNo);

        api_send_json([
            'success' => true,
            'records' => $records
        ]);
    }

    api_send_json(['success' => false, 'message' => 'Not found'], 404);
} catch (Exception $e) {
    log_error("API error: " . $e->getMessage(), 'api');
    api_send_json(['success' => false, 'message' => 'Internal server error'], 500);
}
    if (preg_match('#^students/([^/]+)$#', $path, $matches) && $method === 'GET') {
        $regNo = clean_input(urldecode($matches[1]));
        if ($regNo === '') {
            api_send_json(['success' => false, 'message' => 'Invalid regNo'], 400);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        $student = api_student_by_reg_no($pdo, $regNo);
        if (!$student) {
            api_send_json(['success' => false, 'message' => 'Student not found'], 404);
        }

        $relations = api_load_student_relations($pdo, (int)$student['id']);
        $payload = api_student_to_response($student, $relations);
        $payload['transactions'] = api_load_student_transactions($pdo, $regNo);

        api_send_json(['success' => true, 'student' => $payload]);
    }

    if (preg_match('#^students/([^/]+)/photo$#', $path, $matches) && $method === 'GET') {
        $regNo = clean_input(urldecode($matches[1]));
        if ($regNo === '') {
            api_send_json(['success' => false, 'message' => 'Invalid regNo'], 400);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        $stmt = $pdo->prepare("SELECT passport_path FROM students WHERE reg_no = ? LIMIT 1");
        $stmt->execute([$regNo]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);
        if (!$row || empty($row['passport_path'])) {
            api_send_json(['success' => false, 'message' => 'Photo not found'], 404);
        }

        $filePath = __DIR__ . '/../uploads/' . basename($row['passport_path']);
        if (!is_file($filePath)) {
            api_send_json(['success' => false, 'message' => 'Photo not found'], 404);
        }

        $mime = mime_content_type($filePath) ?: 'application/octet-stream';
        header('Content-Type: ' . $mime);
        readfile($filePath);
        exit;
    }

    if (preg_match('#^enrollment/status/([^/]+)$#', $path, $matches) && $method === 'GET') {
        $regNo = clean_input(urldecode($matches[1]));
        if ($regNo === '') {
            api_send_json(['success' => false, 'message' => 'Invalid regNo'], 400);
        }

        $pdo = get_db_connection();
        if (!$pdo) {
            api_send_json(['success' => false, 'message' => 'Database connection failed'], 500);
        }

        $stmt = $pdo->prepare("SELECT id FROM students WHERE reg_no = ? LIMIT 1");
        $stmt->execute([$regNo]);
        $student = $stmt->fetch(PDO::FETCH_ASSOC);
        if (!$student) {
            api_send_json(['success' => false, 'message' => 'Student not found'], 404);
        }

        $stmt = $pdo->prepare("
            SELECT COUNT(*) AS cnt, MIN(enrolled_at) AS enrolled_at
            FROM fingerprint_enrollment
            WHERE reg_no = ? AND is_active = 1
        ");
        $stmt->execute([$regNo]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);
        $count = (int)($row['cnt'] ?? 0);

        api_send_json([
            'success' => true,
            'regNo' => $regNo,
            'isEnrolled' => $count > 0,
            'fingerCount' => $count,
            'enrolledAt' => $count > 0 ? $row['enrolled_at'] : null,
        ]);
    }
