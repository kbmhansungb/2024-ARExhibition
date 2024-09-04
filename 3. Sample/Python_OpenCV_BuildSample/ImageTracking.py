# OpenCV 이미지 트래킹 예시입니다.
# 트래킹 알고리즘은 KCF를 사용합니다.
import cv2

# 비디오 캡처 객체 생성 (0은 기본 카메라를 의미, 파일 경로를 넣으면 비디오 파일을 사용)
cap = cv2.VideoCapture(0)

# KCF 추적기 생성
tracker = cv2.TrackerKCF_create()

# 첫 번째 프레임에서 추적할 객체 선택
ret, frame = cap.read()
if not ret:
    print("비디오를 읽을 수 없습니다.")
    cap.release()
    cv2.destroyAllWindows()
    exit()

# 사용자가 추적할 객체를 선택하도록 함
bbox = cv2.selectROI("Tracking", frame, False)
cv2.destroyWindow("Tracking")

# 선택한 객체를 KCF 추적기에 초기화
ret = tracker.init(frame, bbox)

while True:
    # 비디오의 각 프레임 읽기
    ret, frame = cap.read()
    if not ret:
        break

    # 추적기 업데이트 (추적 성공 여부와 새로운 바운딩 박스를 반환)
    ret, bbox = tracker.update(frame)

    # 추적 성공 시, 객체의 위치를 사각형으로 그리기
    if ret:
        p1 = (int(bbox[0]), int(bbox[1]))
        p2 = (int(bbox[0] + bbox[2]), int(bbox[1] + bbox[3]))
        cv2.rectangle(frame, p1, p2, (255, 0, 0), 2, 1)
    else:
        cv2.putText(frame, "Tracking failure detected", (100, 80), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (0, 0, 255), 2)

    # 결과를 화면에 표시
    cv2.putText(frame, "KCF Tracker", (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (50, 170, 50), 2)
    cv2.imshow("Tracking", frame)

    # 사용자가 ESC 키를 누르면 루프 종료
    if cv2.waitKey(1) & 0xFF == 27:
        break

# 자원 해제
cap.release()
cv2.destroyAllWindows()
