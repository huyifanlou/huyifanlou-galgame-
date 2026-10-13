// 校园数据：单一数据源——渲染、碰撞、交互检测都从这里生成
// face: 1 = 正面朝 +z；-1 = 正面朝 -z（用于位于场地南侧的建筑）
export const BUILDINGS = [
  { id: 'teach', name: '教学楼', pos: [-12, -10], size: [10, 5, 6], wall: '#b39a7c', roof: '#45465a', cols: 6, rows: 4, face: 1 },
  { id: 'dorm', name: '宿舍楼', pos: [12, -8], size: [8, 4.2, 5], wall: '#9aa0b0', roof: '#3c4252', cols: 5, rows: 3, face: 1 },
  { id: 'library', name: '图书馆', pos: [0, -18], size: [12, 4.6, 6], wall: '#8f7a63', roof: '#4a3f56', cols: 7, rows: 3, face: 1 },
  { id: 'canteen', name: '食堂', pos: [17, 9], size: [9, 3.6, 5], wall: '#b07a58', roof: '#5a3f3a', cols: 5, rows: 2, face: -1 },
]

export const TREES = [
  [-6, -3], [6, -2], [-18, 2], [18, 3], [-22, -14], [22, -14],
  [-8, 8], [8, 9], [-3, -7], [3, -6], [14, 14], [-14, 15],
]
