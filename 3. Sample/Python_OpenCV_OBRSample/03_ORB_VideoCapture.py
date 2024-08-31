import numpy as np
import cv2

# 오일러 각도로 변환하는 함수
def rotation_matrix_to_euler_angles(R):
    sy = np.sqrt(R[0, 0] ** 2 + R[1, 0] ** 2)
    singular = sy < 1e-6

    if not singular:
        x = np.arctan2(R[2, 1], R[2, 2])
        y = np.arctan2(-R[2, 0], sy)
        z = np.arctan2(R[1, 0], R[0, 0])
    else:
        x = np.arctan2(-R[1, 2], R[1, 1])
        y = np.arctan2(-R[2, 0], sy)
        z = 0

    return np.degrees(np.array([x, y, z]))

# 쿼리 이미지 (비교할 객체 이미지)를 읽어옵니다
img1 = cv2.imread('Book_1.png', 0)  # 객체 이미지

# ORB 검출기를 초기화합니다
orb = cv2.ORB_create()

# ORB를 사용하여 키포인트와 디스크립터를 찾습니다
kp1, des1 = orb.detectAndCompute(img1, None)

# ORB를 위한 FLANN 매칭 매개변수 설정 (LSH 인덱스를 사용)
FLANN_INDEX_LSH = 6
index_params = dict(algorithm=FLANN_INDEX_LSH,
                    table_number=6,  # 12 테이블 수
                    key_size=12,     # 20 키 크기
                    multi_probe_level=1)  # 2 다중 탐색 레벨
search_params = dict(checks=50)   # 또는 빈 딕셔너리 전달

flann = cv2.FlannBasedMatcher(index_params, search_params)

# 웹캠에서 실시간으로 프레임을 캡처합니다
cap = cv2.VideoCapture(0)

while True:
    ret, img2 = cap.read()
    if not ret:
        print("웹캠에서 프레임을 캡처할 수 없습니다.")
        break

    # 그레이스케일로 변환
    gray = cv2.cvtColor(img2, cv2.COLOR_BGR2GRAY)

    # 키포인트와 디스크립터 계산
    kp2, des2 = orb.detectAndCompute(gray, None)

    if des2 is not None:
        # 매칭을 수행합니다
        matches = flann.knnMatch(des1, des2, k=2)

        # 좋은 매칭만을 그리기 위해 매칭 마스크를 만듭니다
        good_matches = []
        matchesMask = [[0, 0] for i in range(len(matches))]

        # Lowe의 논문에 따른 비율 테스트
        for i, m_n in enumerate(matches):
            if len(m_n) == 2:  # 매칭 결과가 2개 이상일 때만 처리
                m, n = m_n
                if m.distance < 0.7 * n.distance:
                    matchesMask[i] = [1, 0]
                    good_matches.append(m)

        if len(good_matches) > 4:  # 호모그래피를 찾기 위해서는 최소 4개의 점이 필요합니다
            src_pts = np.float32([kp1[m.queryIdx].pt for m in good_matches]).reshape(-1, 1, 2)
            dst_pts = np.float32([kp2[m.trainIdx].pt for m in good_matches]).reshape(-1, 1, 2)

            # 호모그래피 행렬을 찾습니다
            M, mask = cv2.findHomography(src_pts, dst_pts, cv2.RANSAC, 5.0)

            if M is not None:  # 호모그래피 행렬이 유효한지 확인
                # 쿼리 이미지(img1)의 코너를 가져옵니다
                h, w = img1.shape
                pts = np.float32([[0, 0], [0, h - 1], [w - 1, h - 1], [w - 1, 0]]).reshape(-1, 1, 2)

                # 장면 이미지(img2)로 코너를 투영합니다
                dst = cv2.perspectiveTransform(pts, M)

                # 장면 이미지에 사각형을 그립니다
                img2 = cv2.polylines(img2, [np.int32(dst)], True, (0, 255, 0), 3, cv2.LINE_AA)

                # 실제 객체의 3D 좌표를 정의합니다 (박스의 경우)
                obj_pts = np.float32([[0, 0, 0], [0, h, 0], [w, h, 0], [w, 0, 0]])

                # 카메라 행렬 (내부 파라미터) - 예시를 위해 사용, 실제 카메라 파라미터로 교체하세요
                focal_length = 1.0  # 예시로 픽셀 단위의 초점 거리 사용
                center = (w / 2, h / 2)  # 광학 중심 (이미지의 중심 가정)
                camera_matrix = np.array([[focal_length, 0, center[0]],
                                          [0, focal_length, center[1]],
                                          [0, 0, 1]])

                # 렌즈 왜곡이 없다고 가정 (단순화를 위해)
                dist_coeffs = np.zeros((4, 1))

                # SolvePnP를 사용하여 회전 및 이동 벡터를 찾습니다
                retval, rvec, tvec = cv2.solvePnP(obj_pts, dst, camera_matrix, dist_coeffs)

                # 회전 벡터를 회전 행렬로 변환합니다
                rmat, _ = cv2.Rodrigues(rvec)

                # 회전 행렬을 오일러 각도로 변환합니다
                euler_angles = rotation_matrix_to_euler_angles(rmat)

                print("회전 행렬:\n", rmat)
                print("이동 벡터:\n", tvec)
                print("오일러 각도 (X, Y, Z):\n", euler_angles)

                # 이미지에 위치와 회전 정보 표시
                position_text = "Position: x={:.2f}, y={:.2f}, z={:.2f}".format(tvec[0][0], tvec[1][0], tvec[2][0])
                rotation_text = "Rotation: x={:.2f}, y={:.2f}, z={:.2f}".format(euler_angles[0], euler_angles[1], euler_angles[2])

                # 텍스트 위치
                text_position = (10, img2.shape[0] - 40)
                text_rotation = (10, img2.shape[0] - 10)

                # 텍스트 그리기
                cv2.putText(img2, position_text, text_position, cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)
                cv2.putText(img2, rotation_text, text_rotation, cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)

    # 결과를 화면에 표시합니다
    cv2.imshow('Webcam Object Detection', img2)

    # 'q' 키를 누르면 종료합니다
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# 모든 창을 닫고 캡처를 해제합니다
cap.release()
cv2.destroyAllWindows()
