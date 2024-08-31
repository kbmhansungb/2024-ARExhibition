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

# 여러 이미지를 로드합니다 (예: 이미지 파일명 리스트)
image_filenames = ['Book_1.png', 'Book_2.jpg', 'Book_3.jpg']  # 비교할 이미지 파일명 리스트
images = [cv2.imread(filename, 0) for filename in image_filenames]

# ORB 검출기를 초기화합니다
orb = cv2.ORB_create()

# 각 이미지에 대해 키포인트와 디스크립터를 미리 계산합니다
keypoints_and_descriptors = [(orb.detectAndCompute(img, None)) for img in images]

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
    ret, frame = cap.read()
    if not ret:
        print("웹캠에서 프레임을 캡처할 수 없습니다.")
        break

    # 그레이스케일로 변환
    gray_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)

    # 현재 프레임의 키포인트와 디스크립터 계산
    kp_frame, des_frame = orb.detectAndCompute(gray_frame, None)

    if des_frame is not None:
        # 각 이미지와 비교하여 가장 많은 매칭을 찾습니다
        best_image_index = -1
        max_good_matches = 0
        best_homography = None

        for i, (kp_img, des_img) in enumerate(keypoints_and_descriptors):
            # 매칭을 수행합니다
            matches = flann.knnMatch(des_img, des_frame, k=2)

            # 좋은 매칭만을 그리기 위해 매칭 마스크를 만듭니다
            good_matches = []
            matchesMask = [[0, 0] for _ in range(len(matches))]

            # Lowe의 논문에 따른 비율 테스트
            for j, m_n in enumerate(matches):
                if len(m_n) == 2:  # 매칭 결과가 2개 이상일 때만 처리
                    m, n = m_n
                    if m.distance < 0.7 * n.distance:
                        matchesMask[j] = [1, 0]
                        good_matches.append(m)

            # 가장 많은 좋은 매칭을 가진 이미지를 선택합니다
            if len(good_matches) > max_good_matches:
                max_good_matches = len(good_matches)
                best_image_index = i

                if len(good_matches) > 4:  # 호모그래피를 찾기 위해서는 최소 4개의 점이 필요합니다
                    src_pts = np.float32([kp_img[m.queryIdx].pt for m in good_matches]).reshape(-1, 1, 2)
                    dst_pts = np.float32([kp_frame[m.trainIdx].pt for m in good_matches]).reshape(-1, 1, 2)
                    best_homography, _ = cv2.findHomography(src_pts, dst_pts, cv2.RANSAC, 5.0)

        if best_image_index != -1 and best_homography is not None:
            # 가장 유사한 이미지의 코너를 현재 프레임에 투영하여 그립니다
            h, w = images[best_image_index].shape
            pts = np.float32([[0, 0], [0, h - 1], [w - 1, h - 1], [w - 1, 0]]).reshape(-1, 1, 2)
            dst = cv2.perspectiveTransform(pts, best_homography)

            # 현재 프레임에 사각형을 그립니다
            frame = cv2.polylines(frame, [np.int32(dst)], True, (0, 255, 0), 3, cv2.LINE_AA)

            # 현재 프레임에 가장 유사한 이미지의 이름을 표시합니다
            cv2.putText(frame, image_filenames[best_image_index], (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)

    # 결과를 화면에 표시합니다
    cv2.imshow('Webcam Object Detection', frame)

    # 'q' 키를 누르면 종료합니다
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# 모든 창을 닫고 캡처를 해제합니다
cap.release()
cv2.destroyAllWindows()
