import cv2
import numpy as np
import sys

def detect_lines_and_visualize(image_path, output_path, threshold=160, line_thickness=5, min_line_length=5):
    image = cv2.imread(image_path)
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    _, binary = cv2.threshold(gray, threshold, 255, cv2.THRESH_BINARY)

    lines = cv2.HoughLinesP(binary, 1, np.pi / 180, threshold=5, minLineLength=min_line_length, maxLineGap=10)

    if lines is not None:
        for line in lines:
            x0, y0, x1, y1 = line[0]
            cv2.line(image, (x0, y0), (x1, y1), (0, 255, 0), line_thickness)
            print(f"Start: ({x0}, {y0}), End: ({x1}, {y1})")  # 打印坐标
    else:
        print("没有检测到直线")

    # 保存处理后的图像
    cv2.imwrite(output_path, image)
    print(f"处理后的图像已保存到: {output_path}")

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("用法: python py.py <输入图像路径> <输出图像路径>")
        sys.exit(1)

    input_image_path = sys.argv[1]
    output_image_path = sys.argv[2]
    detect_lines_and_visualize(input_image_path, output_image_path)
