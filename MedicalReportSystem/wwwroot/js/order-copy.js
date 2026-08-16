/**
 * 医嘱复制页面
 * 用于将检验/检查项目的明细复制到病历
 * 通过URL参数 idCard 接收患者身份证号
 */

// ==================== 全局变量 ====================
let currentIdCard = '';
let currentPatientId = '';
let currentDoctorId = '';
let currentPatientName = '';
let labReports = [];      // 检验报告列表
let examReports = [];     // 检查报告列表
let currentOrderId = null;
let currentOrderType = null;  // 'laboratory' 或 'radiology'
let currentBusinessNo = null;
let selectedDetailIds = new Set();
let currentDetailsData = null;
let currentReportIdForExam = null;  // 存储当前检查报告的ID，用于生成固定ID

// DOM 元素
const orderGroupsContainer = document.getElementById('orderGroupsContainer');
const detailContent = document.getElementById('detailContent');
const selectedOrderNameSpan = document.getElementById('selectedOrderName');
const selectedOrderTypeSpan = document.getElementById('selectedOrderType');
const selectedDetailCountSpan = document.getElementById('selectedDetailCount');
const clearSelectionBtn = document.getElementById('clearSelectionBtn');
const copyToRecordBtn = document.getElementById('copyToRecordBtn');

// ==================== 工具函数 ====================
function escapeHtml(str) {
    if (!str) return '';
    return String(str).replace(/[&<>]/g, function (m) {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
}

function padString(str, len) {
    if (!str) str = '';
    if (str.length >= len) return str.substring(0, len);
    return str + ' '.repeat(len - str.length);
}

function updateSelectedCount() {
    if (selectedDetailCountSpan) {
        selectedDetailCountSpan.innerText = selectedDetailIds.size;
        console.log('更新选中数量:', selectedDetailIds.size, '选中ID列表:', Array.from(selectedDetailIds));
    }
}

function clearSelectedDetails() {
    // 清空存储的选中ID集合
    selectedDetailIds.clear();
    updateSelectedCount();

    // 直接操作DOM，清除所有复选框的勾选状态
    const allCheckboxes = document.querySelectorAll('.detail-checkbox, .exam-detail-checkbox');
    allCheckboxes.forEach(checkbox => {
        checkbox.checked = false;
    });

    console.log('已清空所有选中，当前选中数量:', selectedDetailIds.size);
}

// 显示提示消息
//function showMessage(message, type = 'info') {
//    alert(message);
//}
// 显示提示消息（使用项目现有的 Toast 样式）
function showMessage(message, type = 'info') {
    // 获取或创建 toast 容器
    let container = document.querySelector('.left-toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'left-toast-container';
        document.body.appendChild(container);
    }

    // 创建 toast
    const toast = document.createElement('div');
    toast.className = `left-toast toast-${type}`;
    toast.innerHTML = `
        <div class="toast-message">${escapeHtml(message)}</div>
        <div class="toast-close">&times;</div>
        <div class="toast-progress"></div>
    `;

    container.appendChild(toast);

    // 添加显示动画
    setTimeout(() => toast.classList.add('show'), 10);

    // 关闭按钮
    const closeBtn = toast.querySelector('.toast-close');
    closeBtn.addEventListener('click', () => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    });

    // 进度条动画
    const progress = toast.querySelector('.toast-progress');
    progress.style.transition = 'width 3s linear';
    progress.style.width = '100%';
    setTimeout(() => {
        progress.style.width = '0%';
    }, 10);

    // 3秒后自动消失
    setTimeout(() => {
        if (toast.parentNode) {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }
    }, 3000);
}

// ==================== API 调用 ====================
async function fetchData(url) {
    const response = await fetch(url);

    if (!response.ok) {
        if (response.status === 404) {
            return null;
        }
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    // 先获取文本内容
    const text = await response.text();
    console.log('原始响应内容:', text.substring(0, 500)); // 打印前500个字符用于调试

    // 尝试解析 JSON，去除可能的 BOM 头
    try {
        // 去除 BOM 头（\uFEFF）
        const cleanText = text.replace(/^\uFEFF/, '');
        return JSON.parse(cleanText);
    } catch (e) {
        console.error('JSON解析失败:', e);
        console.error('原始内容:', text);
        throw new Error('响应不是有效的JSON格式');
    }
}

// 加载检验报告列表
async function loadLabReports(idCard) {
    const url = `/api/OrderCopy/lab-reports/${encodeURIComponent(idCard)}`;
    const data = await fetchData(url);
    return data || [];
}

// 加载检查报告列表
async function loadExamReports(idCard) {
    const url = `/api/OrderCopy/exam-reports/${encodeURIComponent(idCard)}`;
    const data = await fetchData(url);
    return data || [];
}

// 检验明细接口
async function loadLabDetails(businessNo, reportId) {
    const encodedBusinessNo = encodeURIComponent(businessNo);
    const encodedReportId = encodeURIComponent(reportId);
    const url = `/api/OrderCopy/lab-details/${encodedBusinessNo}/${encodedReportId}`;

    console.log('加载检验明细URL:', url);
    const detail = await fetchData(url);

    if (!detail) {
        return null;
    }

    const indicators = detail.report_testr_res_indicate || [];
    return indicators.map(item => ({
        id: item.ID,
        itemNo: item.dataCode,
        reportNo: item.testReportNo,
        itemName: item.test_proj_name_exp || item.dataName,
        resultValue: item.test_index_result,
        unit: item.test_index_uint,
        abnormalFlag: item.anomalyCode,
        resultDesc: item.anomaly_name || item.testResDescription,
        referenceRange: item.normalRefRes,
        refValue: item.normalRefLimit,
        recognitionScope: item.mtuRecLimitMark
    }));
}

// 加载检查明细
async function loadExamDetails(businessNo, reportId) {
    // 使用 QueryString 传参
    const encodedBusinessNo = encodeURIComponent(businessNo);
    const encodedReportId = encodeURIComponent(reportId);
    const url = `/api/OrderCopy/exam-details/${encodedBusinessNo}/${encodedReportId}`;

    console.log('加载检查明细URL:', url);

    try {
        const detail = await fetchData(url);

        if (!detail) {
            return null;
        }

        // 映射 T_CHECK_REC_oracle 字段到前端需要的格式
        return {
            id: detail.id,
            businessNo: detail.businessNo,
            checkReportNo: detail.checkReportNo,
            dataName: detail.dataName,
            checkReportDate: detail.checkReportDate,
            checkProjName: detail.checkProjName,
            reportDoctName: detail.reportDoctName,
            checkReportDepartName: detail.checkReportDepartName,
            // 报告描述 - 优先使用 checkResObj，其次 checkRes
            findings: detail.checkResObj || detail.checkRes,
            reportDesc: detail.checkRes,
            // 诊断 - 使用 checkResSub 或 diagTypeName
            impression: detail.checkResSub || detail.diagTypeName,
            diagnosis: detail.diagTypeName,
            // 检查方法
            studyDesc: detail.checkProjName,
            // 报告备注
            reportComment: detail.checkReportComment
        };
    } catch (error) {
        console.error('加载检查明细失败:', error);
        return null;
    }
}
// ==================== 更新页面标题 ====================
function updatePageTitle(patientName) {
    const titleElement = document.querySelector('.order-list-header h3');
    if (titleElement && patientName) {
        titleElement.innerHTML = `📋 ${escapeHtml(patientName)}的外院医嘱列表`;
    } else if (titleElement) {
        titleElement.innerHTML = `📋 医嘱列表`;
    }

    // 同时更新浏览器标签页标题
    if (patientName) {
        document.title = `${patientName}的外院医嘱列表 - 医嘱复制`;
    } else {
        document.title = `医嘱复制 - 外院医嘱列表`;
    }
}

// ==================== 渲染左侧列表 ====================
function renderOrderGroups() {
    if (!orderGroupsContainer) return;
    orderGroupsContainer.innerHTML = '';

    // 检验分组
    if (labReports.length > 0) {
        const labGroup = createOrderGroup('🔬 检验', 'laboratory', labReports, (report) => ({
            id: report.id,
            name: report.testProjCategoryName || report.reportName || '未知检验',
            date: report.testReportDate || '',
            time: report.reportTime || '',
            doctor: report.reportDoctName || '',
            department: report.deptName || '',
            businessNo: report.businessNo
        }));
        orderGroupsContainer.appendChild(labGroup);
    }

    // 检查分组
    if (examReports.length > 0) {
        const radGroup = createOrderGroup('🩻 检查', 'radiology', examReports, (report) => ({
            id: report.id,
            name: report.checkProjNameExp || report.reportName || '未知检查',
            date: report.checkReportDate || '',
            doctor: report.reportDoctName || '',
            department: report.checkReportDepartName || '',
            businessNo: report.businessNo
        }));
        orderGroupsContainer.appendChild(radGroup);
    }

    if (labReports.length === 0 && examReports.length === 0) {
        orderGroupsContainer.innerHTML = '<div class="empty-state">📭 暂无检验或检查报告数据</div>';
    }
}

function createOrderGroup(title, groupType, reports, getDisplayInfo) {
    const groupDiv = document.createElement('div');
    groupDiv.className = 'order-group expanded';
    groupDiv.dataset.groupType = groupType;

    // 分组头部
    const groupHeader = document.createElement('div');
    groupHeader.className = 'order-group-header';
    groupHeader.innerHTML = `
        <span class="group-title">${title}</span>
        <span class="group-count">${reports.length}</span>
    `;
    groupHeader.addEventListener('click', (e) => {
        e.stopPropagation();
        groupDiv.classList.toggle('expanded');
    });

    // 分组内容
    const groupContent = document.createElement('div');
    groupContent.className = 'order-group-content';

    reports.forEach(report => {
        const info = getDisplayInfo(report);
        const orderItem = document.createElement('div');
        orderItem.className = 'order-item';
        if (currentOrderId === info.id && currentOrderType === groupType) {
            orderItem.classList.add('active');
        }
        orderItem.dataset.orderId = info.id;
        orderItem.dataset.orderType = groupType;
        orderItem.dataset.businessNo = info.businessNo;
        orderItem.dataset.reportName = info.name;

        // 构建日期时间显示
        let dateTimeStr = info.date;
        if (info.time && info.time !== info.date) {
            dateTimeStr = info.time;
        }

        orderItem.innerHTML = `
            <div class="order-content">
                <div class="order-name">${escapeHtml(info.name)}</div>
                <div class="order-meta">
                    <span>📅 ${escapeHtml(dateTimeStr || '-')}</span>
                    <span>👨‍⚕️ ${escapeHtml(info.doctor || '-')}</span>
                    <span>🏥 ${escapeHtml(info.department || '-')}</span>
                </div>
            </div>
        `;

        orderItem.addEventListener('click', () => {
            // 移除其他高亮
            document.querySelectorAll('.order-item').forEach(item => item.classList.remove('active'));
            orderItem.classList.add('active');

            // 更新当前选中状态
            currentOrderId = info.id;
            currentOrderType = groupType;
            currentBusinessNo = info.businessNo;
            currentReportIdForExam = info.id;  // 存储报告ID用于生成固定ID

            selectedOrderNameSpan.innerText = info.name;
            selectedOrderTypeSpan.innerText = groupType === 'laboratory' ? '检验项目明细' : '检查报告明细';

            // 清空之前选中的明细
            selectedDetailIds.clear();
            updateSelectedCount();

            // 加载明细
            if (groupType === 'laboratory') {
                loadAndRenderLabDetails(currentBusinessNo, currentOrderId);
            } else {
                loadAndRenderExamDetails(currentBusinessNo, currentOrderId);
            }
        });

        groupContent.appendChild(orderItem);
    });

    groupDiv.appendChild(groupHeader);
    groupDiv.appendChild(groupContent);
    return groupDiv;
}

// ==================== 渲染检验明细 ====================
async function loadAndRenderLabDetails(businessNo, reportId) {
    detailContent.innerHTML = '<div class="loading-state">⏳ 加载检验明细...</div>';

    try {
        const data = await loadLabDetails(businessNo, reportId);
        currentDetailsData = data;

        if (data && data.length > 0) {
            renderLaboratoryDetails(data);
        } else {
            detailContent.innerHTML = '<div class="empty-state">📊 暂无检验明细数据</div>';
        }
    } catch (error) {
        console.error('加载检验明细失败:', error);
        detailContent.innerHTML = `<div class="empty-state">❌ 加载失败: ${escapeHtml(error.message)}<br><button onclick="loadAndRenderLabDetails('${businessNo}','${reportId}')" style="margin-top:10px;padding:5px 15px;cursor:pointer;">重试</button></div>`;
    }
}

function renderLaboratoryDetails(details) {
    const table = document.createElement('table');
    table.className = 'detail-table';
    table.innerHTML = `
        <thead>
            <tr>
                <th class="detail-check-col">选择</th>
                <th>检验项目</th>
                <th>结果</th>
                <th>单位</th>
                <th>结果描述</th>
                <th>参考值</th>
                <th>互认范围标识</th>
            </tr>
        </thead>
        <tbody id="detailTableBody"></tbody>
    `;

    const tbody = table.querySelector('#detailTableBody');

    details.forEach(detail => {
        // 使用 detail.id 作为唯一标识，如果没有则使用索引
        const itemId = detail.id || `lab_${currentOrderId}_${detail.itemNo || detail.itemName}`;
        const isSelected = selectedDetailIds.has(itemId);

        const itemName = detail.itemName || detail.projectName || '-';
        const resultValue = detail.resultValue || detail.result || '';
        const unit = detail.unit || '-';
        const abnormalFlag = detail.abnormalFlag || detail.resultDesc || '';
        const refRange = detail.referenceRange || detail.refValue || '-';
        const recognitionScope = detail.recognitionScope || '广西';

        // 判断是否异常
        const isAbnormal = abnormalFlag === '偏高' || abnormalFlag === '偏低' ||
            abnormalFlag === 'H' || abnormalFlag === 'L' ||
            abnormalFlag === '↑' || abnormalFlag === '↓';
        const valueClass = isAbnormal ? 'value-abnormal' : 'value-normal';
        const descClass = isAbnormal ? 'value-abnormal' : 'value-normal';

        const row = document.createElement('tr');
        row.dataset.detailId = itemId;
        row.innerHTML = `
            <td class="detail-check-col">
                <input type="checkbox" class="detail-checkbox" data-id="${escapeHtml(itemId)}" ${isSelected ? 'checked' : ''}>
            </td>
            <td>${escapeHtml(itemName)}</td>
            <td class="${valueClass}">${escapeHtml(resultValue)}</td>
            <td>${escapeHtml(unit)}</td>
            <td class="${descClass}">${escapeHtml(abnormalFlag || '正常')}</td>
            <td>${escapeHtml(refRange)}</td>
            <td><span class="recognition-badge">${escapeHtml(recognitionScope)}</span></td>
        `;

        const checkbox = row.querySelector('.detail-checkbox');
        checkbox.addEventListener('change', (e) => {
            e.stopPropagation();
            if (checkbox.checked) {
                selectedDetailIds.add(itemId);
            } else {
                selectedDetailIds.delete(itemId);
            }
            updateSelectedCount();
        });

        tbody.appendChild(row);
    });

    detailContent.innerHTML = '';
    detailContent.appendChild(table);
    updateSelectedCount();
}

// ==================== 渲染检查明细 ====================
async function loadAndRenderExamDetails(businessNo, reportId) {
    detailContent.innerHTML = '<div class="loading-state">⏳ 加载检查报告...</div>';

    try {
        const data = await loadExamDetails(businessNo, reportId);
        currentDetailsData = data;

        if (data && (data.findings || data.impression || data.reportDesc || data.diagnosis)) {
            renderRadiologyDetails(data);
        } else {
            detailContent.innerHTML = '<div class="empty-state">📋 暂无检查报告数据</div>';
        }
    } catch (error) {
        console.error('加载检查明细失败:', error);
        detailContent.innerHTML = `<div class="empty-state">❌ 加载失败: ${escapeHtml(error.message)}<br><button onclick="loadAndRenderExamDetails('${businessNo}','${reportId}')" style="margin-top:10px;padding:5px 15px;cursor:pointer;">重试</button></div>`;
    }
}

function renderRadiologyDetails(data) {
    const container = document.createElement('div');
    container.className = 'exam-detail-container';

    // 使用固定的报告ID生成唯一标识，而不是时间戳
    const baseId = `exam_${currentReportIdForExam || currentOrderId}`;

    // 报告描述
    const findings = data.findings || data.reportDesc;
    if (findings) {
        const descId = `${baseId}_findings`;
        const isSelected = selectedDetailIds.has(descId);
        const descCard = createExamCard('报告描述', findings, descId, isSelected);
        container.appendChild(descCard);
    }

    // 诊断结论
    const impression = data.impression || data.diagnosis;
    if (impression) {
        const diagId = `${baseId}_diagnosis`;
        const isSelected = selectedDetailIds.has(diagId);
        const diagCard = createExamCard('报告诊断', impression, diagId, isSelected);
        container.appendChild(diagCard);
    }

    // 检查方法和部位
    if (data.studyDesc) {
        const methodId = `${baseId}_method`;
        const isSelected = selectedDetailIds.has(methodId);
        const methodCard = createExamCard('检查方法', data.studyDesc, methodId, isSelected);
        container.appendChild(methodCard);
    }

    if (container.children.length === 0) {
        detailContent.innerHTML = '<div class="empty-state">📋 暂无检查报告内容</div>';
        return;
    }

    detailContent.innerHTML = '';
    detailContent.appendChild(container);
    updateSelectedCount();
}

function createExamCard(label, value, id, isSelected) {
    const card = document.createElement('div');
    card.className = 'exam-detail-card';
    card.dataset.detailId = id;

    card.innerHTML = `
        <div style="display: flex; align-items: flex-start; gap: 15px;">
            <div class="detail-check-col" style="margin-top: 2px;">
                <input type="checkbox" class="exam-detail-checkbox" data-id="${escapeHtml(id)}" ${isSelected ? 'checked' : ''}>
            </div>
            <div style="flex: 1;">
                <div class="exam-detail-label">${escapeHtml(label)}</div>
                <div class="exam-detail-value">${escapeHtml(value).replace(/\n/g, '<br>')}</div>
            </div>
        </div>
    `;

    const checkbox = card.querySelector('.exam-detail-checkbox');
    checkbox.addEventListener('change', (e) => {
        e.stopPropagation();
        if (checkbox.checked) {
            selectedDetailIds.add(id);
        } else {
            selectedDetailIds.delete(id);
        }
        updateSelectedCount();
    });

    return card;
}


// ==================== 记录医嘱引用 ====================
async function recordReference(reportId) {
    if (!currentPatientId || !currentDoctorId || !reportId) {
        console.log('缺少必要参数，跳过记录引用:', {
            patientId: currentPatientId,
            doctorId: currentDoctorId,
            reportId: reportId
        });
        return false;
    }

    try {
        const url = `/api/OrderCopy/record-reference`;
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                patientId: currentPatientId,
                doctorId: currentDoctorId,
                reportId: reportId
            })
        });

        const result = await response.json();

        if (result.success) {
            console.log('医嘱引用记录成功:', result);
            return true;
        } else {
            console.error('医嘱引用记录失败:', result.message);
            return false;
        }
    } catch (error) {
        console.error('医嘱引用记录异常:', error);
        return false;
    }
}
// ==================== 复制到病历 ====================
async function copyToMedicalRecord() {
    if (selectedDetailIds.size === 0) {
        showMessage('请至少选择一条明细项', 'warning');
        return;
    }

    const orderName = selectedOrderNameSpan.innerText;
    const orderType = currentOrderType === 'laboratory' ? '检验' : '检查';
    const reportDate = formatReportDate();
    const currentReportId = currentOrderId;  // 当前选中的报告ID

    let content = '';

    if (currentOrderType === 'laboratory' && currentDetailsData) {
        const selectedItems = [];

        currentDetailsData.forEach(detail => {
            const itemId = detail.id || `lab_${currentOrderId}_${detail.itemNo || detail.itemName}`;
            if (selectedDetailIds.has(itemId)) {
                const itemName = detail.itemName || detail.projectName || '-';
                const resultValue = detail.resultValue || detail.result || '-';
                const unit = detail.unit || '';

                selectedItems.push({
                    name: itemName,
                    result: resultValue,
                    unit: unit
                });
            }
        });

        if (selectedItems.length > 0) {
            content = `${reportDate} ${orderName}：`;

            const itemStrings = selectedItems.map(item => {
                const unitStr = item.unit ? ` ${item.unit}` : '';
                return `${item.name} ${item.result}${unitStr}`;
            });

            content += itemStrings.join('、');
            content += '。';
        }

    } else if (currentOrderType === 'radiology' && currentDetailsData) {
        const baseId = `exam_${currentReportIdForExam || currentOrderId}`;
        const selectedItems = [];

        if (selectedDetailIds.has(`${baseId}_findings`)) {
            const findings = currentDetailsData.findings || currentDetailsData.reportDesc;
            if (findings) {
                selectedItems.push({
                    name: '报告描述',
                    value: findings
                });
            }
        }

        if (selectedDetailIds.has(`${baseId}_diagnosis`)) {
            const diagnosis = currentDetailsData.impression || currentDetailsData.diagnosis;
            if (diagnosis) {
                selectedItems.push({
                    name: '报告诊断',
                    value: diagnosis
                });
            }
        }

        if (selectedDetailIds.has(`${baseId}_method`)) {
            if (currentDetailsData.studyDesc) {
                selectedItems.push({
                    name: '检查方法',
                    value: currentDetailsData.studyDesc
                });
            }
        }

        if (selectedItems.length > 0) {
            content = `${reportDate} ${orderName}：`;

            const itemStrings = selectedItems.map(item => {
                return `${item.name}：${item.value}`;
            });

            content += itemStrings.join('；');
            content += '。';
        }
    }

    if (!content) {
        showMessage('没有可复制的明细内容', 'warning');
        return;
    }

    console.log('准备复制的内容:', content);
    // 复制到剪贴板
    let copySuccess = false;
    // 方法1：尝试使用 Clipboard API
    if (navigator.clipboard && window.isSecureContext) {
        try {
            await navigator.clipboard.writeText(content);
            console.log('Clipboard API 复制成功');
            copySuccess = true;
            showMessage(`✅ 已复制 ${selectedDetailIds.size} 条医嘱明细到剪贴板`, 'success');
            //return;
        } catch (err) {
            console.error('Clipboard API 复制失败:', err);
            // 如果 Clipboard API 失败，尝试 fallback 方法
        }
    }
    // 复制成功后，记录引用（如果复制成功且有必要的参数）
    if (copySuccess && currentPatientId && currentDoctorId && currentReportId) {
        await recordReference(currentReportId);
    } else if (copySuccess) {
        console.log('缺少必要参数，跳过记录引用:', {
            patientId: currentPatientId,
            doctorId: currentDoctorId,
            reportId: currentReportId
        });
    }
    // 方法2：使用 execCommand 作为 fallback（兼容旧浏览器）
    try {
        const textarea = document.createElement('textarea');
        textarea.value = content;
        textarea.style.position = 'fixed';
        textarea.style.top = '-9999px';
        textarea.style.left = '-9999px';
        document.body.appendChild(textarea);
        textarea.select();
        textarea.setSelectionRange(0, content.length);

        const success = document.execCommand('copy');
        document.body.removeChild(textarea);

        if (success) {
            console.log('execCommand 复制成功');
            showMessage(`✅ 已复制 ${selectedDetailIds.size} 条医嘱明细到剪贴板`, 'success');
        } else {
            throw new Error('execCommand 复制失败');
        }
    } catch (err) {
        console.error('所有复制方法都失败:', err);
        showMessage(`❌ 复制失败，请手动复制以下内容：\n${content.substring(0, 200)}...`, 'error');
        // 同时输出到控制台
        console.log('完整复制内容:\n', content);
    }
}

/**
 * 格式化报告时间
 */
function formatReportDate() {
    let reportDate = '';

    if (currentOrderType === 'laboratory') {
        const report = labReports.find(r => r.reportNo === currentOrderId || r.id === currentOrderId);
        if (report) {
            reportDate = report.testReportDate || report.reportDate || '';
        }
    } else if (currentOrderType === 'radiology') {
        const report = examReports.find(r => r.id === currentOrderId || r.reportId === currentOrderId);
        if (report) {
            reportDate = report.checkReportDate || report.reportDate || '';
        }
    }

    if (reportDate) {
        let formattedDate = reportDate;
        if (formattedDate.includes('T')) {
            formattedDate = formattedDate.split('T')[0];
        } else if (formattedDate.includes(' ')) {
            formattedDate = formattedDate.split(' ')[0];
        }
        return formattedDate;
    }

    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}
// ==================== 初始化页面 ====================
async function initPage() {
    const urlParams = new URLSearchParams(window.location.search);
    currentIdCard = urlParams.get('idCard');
    currentPatientId = urlParams.get('patientId') || '';
    currentDoctorId = urlParams.get('doctorId') || '';
    currentPatientName = urlParams.get('patientName') || '';

    console.log('初始化参数:', {
        idCard: currentIdCard,
        patientId: currentPatientId,
        doctorId: currentDoctorId,
        patientName: currentPatientName
    });

    // 更新页面标题（使用传入的患者姓名）
    updatePageTitle(currentPatientName);

    if (!currentIdCard) {
        orderGroupsContainer.innerHTML = '<div class="empty-state">⚠️ 缺少患者标识，请通过正确入口访问</div>';
        return;
    }

    orderGroupsContainer.innerHTML = '<div class="loading-state">⏳ 加载患者报告列表...</div>';

    try {
        // 并行加载检验和检查报告
        const [labs, exams] = await Promise.all([
            loadLabReports(currentIdCard),
            loadExamReports(currentIdCard)
        ]);

        labReports = labs;
        examReports = exams;

        renderOrderGroups();

        // 如果有报告，默认选中第一个
        if (labReports.length > 0) {
            const firstItem = document.querySelector('.order-item[data-order-type="laboratory"]');
            if (firstItem) {
                firstItem.click();
            }
        } else if (examReports.length > 0) {
            const firstItem = document.querySelector('.order-item[data-order-type="radiology"]');
            if (firstItem) {
                firstItem.click();
            }
        }
    } catch (error) {
        console.error('初始化失败:', error);
        orderGroupsContainer.innerHTML = `<div class="empty-state">❌ 加载失败: ${escapeHtml(error.message)}<br><button onclick="initPage()" style="margin-top:10px;padding:5px 15px;cursor:pointer;">重试</button></div>`;
    }
}

// ==================== 事件绑定 ====================
if (clearSelectionBtn) {
    clearSelectionBtn.addEventListener('click', clearSelectedDetails);
}
if (copyToRecordBtn) {
    copyToRecordBtn.addEventListener('click', copyToMedicalRecord);
}

// 启动页面
initPage();