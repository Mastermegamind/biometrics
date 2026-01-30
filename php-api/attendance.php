<?php
// php-api/attendance.php

declare(strict_types=1);

header('Content-Type: application/json');

if (($_SERVER['REQUEST_METHOD'] ?? 'GET') !== 'GET') {
    http_response_code(405);
    echo json_encode(['success' => false, 'message' => 'Method not allowed']);
    exit;
}

$pdo = require __DIR__ . '/db.php';

$from = trim((string)($_GET['from'] ?? ''));
$to = trim((string)($_GET['to'] ?? ''));
$regno = trim((string)($_GET['regno'] ?? ''));

if ($from === '' || $to === '') {
    http_response_code(400);
    echo json_encode(['success' => false, 'message' => 'from and to are required']);
    exit;
}

$sql = 'SELECT id, regno, name, class_name, date, time_in, time_out
        FROM attendance_records
        WHERE date >= :from AND date <= :to';
$params = [':from' => $from, ':to' => $to];

if ($regno !== '') {
    $sql .= ' AND regno = :regno';
    $params[':regno'] = $regno;
}

$sql .= ' ORDER BY date DESC, time_in DESC';

$stmt = $pdo->prepare($sql);
$stmt->execute($params);
$rows = $stmt->fetchAll();

$records = [];
foreach ($rows as $row) {
    $records[] = [
        'id' => (int)$row['id'],
        'regNo' => $row['regno'],
        'name' => $row['name'],
        'className' => $row['class_name'],
        'date' => $row['date'],
        'timeIn' => $row['time_in'],
        'timeOut' => $row['time_out']
    ];
}

http_response_code(200);
echo json_encode($records);
