// 全局变量
let currentReports = [];
let currentUserId = null;
let PatientID = null;//病人ID
let currentMode = 'recognize'; // 'recognize'互认模式 或 'view'查阅模式
let currentReportType = 'lab'; // 'lab'检验或'exam'检查，表示当前显示的报告类型
let searchType = '1'; //1：接诊历史提醒2：开单重复提醒
let targetreportId = null;

// 页面加载初始化
document.addEventListener('DOMContentLoaded', async function () {
    const urlParams = new URLSearchParams(window.location.search);
    PatientID = urlParams.get('PatientID'); // 获取地址栏传入patientId
    const initialReportId = urlParams.get('reportId');
    const initialReportType = urlParams.get('type') || 'lab'; // 默认检验报告
    const mode = urlParams.get('mode') || 'recognize'; // 获取模式参数

    // 设置当前模式
    currentMode = mode;



    // 设置当前报告类型
    currentReportType = initialReportType;
    // 设置切换按钮状态
    document.getElementById('lab-switch').classList.toggle('active', initialReportType === 'lab');
    document.getElementById('exam-switch').classList.toggle('active', initialReportType === 'exam');

    // 添加切换按钮事件监听
    document.getElementById('lab-switch').addEventListener('click', () => switchReportType('lab'));
    document.getElementById('exam-switch').addEventListener('click', () => switchReportType('exam'));
    //加载用户信息
    await loadUserPatientDetails(PatientID)
    // 加载报告列表
    await loadUserReportList(PatientID, initialReportType);
    // 根据模式调整UI
    adjustUIForMode();
    // 如果是查阅模式，上传提醒记录
    if (currentMode === 'view') {
        await uploadReminderRecords();
    }
    // 如果有初始报告ID，加载该报告
    if (initialReportId) {
        highlightAndLoadReport(initialReportId);
        //const hospitalReports = currentReports.filter(r =>
        //r.id === initialReportId
        //);
        //loadReportDetails(hospitalReports[0].testReportNo, initialReportId, initialReportType)
    } else if (currentReports.length > 0) {
        // 否则加载第一个报告
        const firstReport = currentReports[0];
        loadReportDetails(firstReport.testReportNo, firstReport.id, initialReportType);
    }
});
// 根据模式调整UI
function adjustUIForMode() {
    const recognitionButtons = document.querySelectorAll('.recognition-btn, .recognition-actions');
    if (currentMode === 'view') {
        updateUrlParams({ searchType:'1' })
        // 隐藏所有互认相关按钮
        recognitionButtons.forEach(btn => {
            btn.style.display = 'none';
        });

        // 可以添加查阅模式特有的UI元素
        //document.querySelector('.page-title').textContent += ' (查阅模式)';
    } else {
        // 显示互认按钮
        updateUrlParams({ searchType: '2' })
        recognitionButtons.forEach(btn => {
            btn.style.display = '';
        });
    }
}
// 上传提醒记录（同时处理检验和检查数据）
async function uploadReminderRecords() {
    try {
        const urlParams = new URLSearchParams(window.location.search);
        const ghId = urlParams.get('GHId') || '0';

        // 1. 获取检验数据
        const labReports = await fetchReports(PatientID, 'lab');
        console.log('检验报告数据:', labReports);
        // 2. 获取检查数据
        const examReports = await fetchReports(PatientID, 'exam');
        console.log('检查报告数据:', examReports);
        // 3. 合并数据并准备提醒记录
        const reminderData = [
            ...prepareReminderData(labReports, 'lab'),
            ...prepareReminderData(examReports, 'exam')
        ];
        console.log('合并后的提醒记录数据:', reminderData);
        if (reminderData.length === 0) {
            console.log('没有需要上传的提醒记录');
            return;
        }

        // 4. 调用提醒记录上传接口
        const response = await fetch('/api/Person/upload-reminder-records', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(reminderData)
        });

        if (!response.ok) {
            throw new Error('上传提醒记录失败');
        }

        const result = await response.json();
        if (result.success) {
            showToast(`提醒记录上传成功，共${reminderData.length}条`);
        } else {
            throw new Error(result.message);
        }
    } catch (error) {
        console.error('上传提醒记录失败:', error);
    }
}
// 获取单条明细数据
async function fetchOrderDetail(ReportID) {
    try {
        const urlParams = new URLSearchParams(window.location.search);
        const ghId = urlParams.get('GHId') || '0';

        // 1. 获取检验数据
        const labReports = await fetchReports(PatientID, 'lab');
        console.log('检验报告数据:', labReports);
        // 2. 获取检查数据
        const examReports = await fetchReports(PatientID, 'exam');
        console.log('检查报告数据:', examReports);
        // 3. 合并数据并准备提醒记录
        const reminderData = [
            ...prepareReminderData(labReports, 'lab'),
            ...prepareReminderData(examReports, 'exam')
        ];
        console.log('合并后的提醒记录数据:', reminderData);
        if (reminderData.length === 0) {
            console.log('没有需要上传的提醒记录');
            return;
        }


        showToast(`提醒记录上传成功，共${reminderData.length}条`);
    } catch (error) {
        console.error('上传提醒记录失败:', error);
    }
}
// 获取报告列表
async function fetchReports(patientId, type) {
    try {
        const listUrl = new URL('/api/Products/detail', window.location.origin);
        listUrl.searchParams.append('userId', patientId);
        listUrl.searchParams.append('type', type);

        const response = await fetch(listUrl);
        if (!response.ok) throw new Error(`获取${type === 'lab' ? '检验' : '检查'}列表失败`);

        return await response.json();
    } catch (error) {
        console.error(`获取${type === 'lab' ? '检验' : '检查'}列表失败:`, error);
        return [];
    }
}
// 准备提醒记录数据
// 修改prepareReminderData函数以处理不同数据结构
function prepareReminderData(reports, type) {
    if (!reports || reports.length === 0) return [];
    const urlParams = new URLSearchParams(window.location.search);
    var PatientID = urlParams.get('PatientID');
    return reports.map(report => {
        // 处理检查数据
        if (type === 'exam') {
            return {
                //patientId: report.patientId || PatientID,
                //测试数据
                patientId: PatientID,
                ghId: urlParams.get('GHId') || '0',
                reportId: report.id || report.checkRecNo,
                reportType: type,
                orgCode: report.orgCode,
                orgName: report.orgName || report.checkReportOrgName,
                reportName: report.checkProjNameExp || report.checkProjName,
                reportTime: formatDateTime(report.checkReportDate),
                searchType: urlParams.get('searchType') || '0',
                reportNo: report.checkReportNo,
                dataCode: report.dataCode,
                idCardValue: report.idCardValue,
                businessNo: report.businessNo
            };
        }
        // 处理检验数据
        else if (type === 'lab') {
            return {
                patientId: PatientID,
                ghId: urlParams.get('GHId') || '0',
                reportId: report.id,
                reportType: type,
                orgCode: report.orgCode,
                orgName: report.orgName || report.testReportOrgName,
                reportName: report.testProjCategoryName,
                reportTime: formatDateTime(report.testReportDate),
                searchType: urlParams.get('searchType') || '0',
                reportNo: report.testReportNo,
                dataCode: report.dataCode,
                idCardValue: report.idCardValue,
                businessNo: report.businessNo
            };
        }
        return null;
    }).filter(item => item !== null);
}
// 加载用户信息
async function loadUserPatientDetails(userId) {
    try {
        if (!userId) {
            console.error('患者ID不能为空');
            return;
        }
        //测试数据固定写死
        //if (userId == 20816) {
        //    userId = 4602748
        //}
        // 调用API获取患者信息
        const response = await fetch(`/api/Person/PatientDetails/${userId}`);

        if (!response.ok) {
            throw new Error(`获取患者信息失败: ${response.status}`);
        }

        const patientData = await response.json();

        // 检查是否有数据
        if (!patientData || patientData.length === 0) {
            console.warn('未找到患者信息');
            return;
        }

        // 取第一条数据
        const patient = patientData[0];

        // 更新患者信息到页面顶部
        document.getElementById('patient-name').textContent = patient.name || '-';
        document.getElementById('patient-gender').textContent = patient.gender || '-';
        document.getElementById('patient-phone').textContent = patient.mobileNumber || '-';
        document.getElementById('patient-id').textContent = patient.idNumber || '-';
        document.getElementById('patient-address').textContent = patient.homeAddress || '-';
        document.getElementById('patient-age').textContent = calculateAge(patient.birthDate) || '-';
    } catch (error) {
        console.error('加载患者信息失败:', error);
        showErrorMessage('patient-header', `加载患者信息失败: ${error.message}`);
    }
}

// 在加载报告列表时获取互认状态
async function loadUserReportList(userId, type = 'lab') {
    try {
        showLoading('reportItems');
        const listUrl = new URL('/api/Products/detail', window.location.origin);//旧版Gbase接口
        //const listUrl = new URL('/api/Person/report-headers-oracle', window.location.origin);
        listUrl.searchParams.append('userId', userId);
        listUrl.searchParams.append('type', type);

        const listResponse = await fetch(listUrl);
        if (!listResponse.ok) throw new Error('列表加载失败');

        let reports = await listResponse.json();
        console.log('API返回的报告数据结构:', reports); // 调试
        // 确保我们处理的是数组
        const reports = Array.isArray(reports) ? reports : [reports];
        // 获取互认状态（适配不同ID字段名）
        reports = await Promise.all(reports.map(async report => {
            const reportId = report.id || report.Id || report.reportId; // 兼容不同命名
            if (!reportId) {
                console.error('无法获取报告ID:', report);
                return { ...report, isRecognized: false };
            }
            try {
                const statusResponse = await fetch(`/api/Person/recognition-status/${userId}/${reportId}`);
                if (statusResponse.ok) {
                    const status = await statusResponse.json();
                    return { ...report, isRecognized: status.isRecognized };
                }
            } catch (e) {
                console.error('获取互认状态失败:', e);
            }
            return { ...report, isRecognized: false };
        }));

        currentReports = reports;
        renderReportList(currentReports, type);
        updateUrlParams({ type });
    } catch (error) {
        console.error('加载失败:', error);
        showErrorMessage('reportItems', `无法加载${type === 'lab' ? '检验' : '检查'}列表：${error.message}`);
    }
}

// 加载检验详情（严格优先级控制）
async function loadReportDetails(TestReportNo, reportId, type) {
    try {
        showLoading('reportDetails');
        if (type === 'lab') {
            // 1. 必须加载表头数据
            const headerData = await fetchHeaderData(reportId);

            // 2. 尝试加载常规检验数据
            const detailsData = await fetchDetailsData(TestReportNo, reportId);
            if (detailsData && detailsData.length > 0) {
                renderReportDetails(headerData, detailsData, null, null);
                return;
            }

            // 3. 常规检验无数据，尝试加载微生物报告
            const microbialData = await fetchCombinedMicrobialData(TestReportNo, reportId);
            if (microbialData && (microbialData.bacteria || microbialData.drugs)) {
                renderReportDetails(headerData, null, microbialData, null);
                return;
            }

            // 4. 显示无数据提示
            renderNoData(headerData);
        } else {
            // 检查报告加载逻辑
            const examData = await fetchExamData(reportId);
            renderExamReportDetails(examData);
        }
    } catch (error) {
        console.error('加载详情失败:', error);
        showErrorMessage('reportDetails', `
            加载失败: ${error.message}
            <button onclick="loadReportDetails('${TestReportNo}','${reportId}','${type}')">点击重试</button>
        `);
    }
}
// 加载检验列表
async function loadReportList() {
    try {
        //const response = await fetch('/Data/SimulatedData.json');
        //if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const response = await fetch('/api/reports/list');//使用API获取检验列表
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        currentReports = await response.json();
        renderReportList(currentReports);
    } catch (error) {
        console.error('加载检验列表失败:', error);
        showErrorMessage('reportItems', '检验列表加载失败，请刷新重试');
    }
}

// 全局定义 handleBatchRecognition
async function handleBatchRecognition(hospitalName, isRecognize) {
    try {
        const urlParams = new URLSearchParams(window.location.search);
        const ghId = urlParams.get('GHId') || '0';

        const hospitalReports = currentReports.filter(r =>
            r.orgName === hospitalName &&
            r.isRecognized !== '1' && 
            r.isRecognized !== '0'
        );

        if (hospitalReports.length === 0) {
            showToast('没有可操作的报告', 'info');
            return;
        }

        // 如果是互认操作，直接执行
        if (isRecognize) {
            if (!confirm(`确定要全部互认 ${hospitalName} 的 ${hospitalReports.length} 份报告吗？`)) {
                return;
            }
        } 
        // 如果是不互认操作，先选择原因
        else {
            const reasonData = await showReasonDialog();
            if (!reasonData) return; // 用户取消了
            
            if (!confirm(`确定要全部不互认 ${hospitalName} 的 ${hospitalReports.length} 份报告吗？`)) {
                return;
            }
        }

        showLoading('reportItems');
        const RecognitionData = prepareRecognitionData(hospitalReports, currentReportType, isRecognize ? null : reasonData);
        
        let response;
        if (isRecognize) {
            response = await PLupdateRemoRecognitionStatus(RecognitionData);
        } else {
            response = await PLupdateRecognitionStatus(RecognitionData);
        }
        
        const result = await response.json();
        if (result.success) {
            showToast(`成功${isRecognize ? '互认' : '不互认'} ${hospitalReports.length}份报告`);
        } else {
            throw new Error(result.message);
        }
        
        await loadUserReportList(PatientID, currentReportType);
    } catch (error) {
        await loadUserReportList(PatientID, currentReportType);
        console.error('批量操作失败:', error);
        showToast('批量操作失败: ' + error.message, 'error');
        return { success: false, error: error.message };
    }
}
// 批量不互认状态
async function PLupdateRecognitionStatus(reminderData) {
    const response = await fetch('/api/Person/submit-batch-recognition', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(reminderData)
    });
    if (!response.ok) throw new Error('批量上传不互认记录失败');
    return response; // 返回响应对象
}
// 批量互认状态
async function PLupdateRemoRecognitionStatus(reminderData) {
    const response = await fetch('/api/Person/PLupload-view-record', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(reminderData)
    });
    if (!response.ok) throw new Error('批量上传互认记录失败');
    return response; // 返回响应对象
}
// 撤销互认状态
async function undoRecognition(reminderData) {
    const response = await fetch('/api/Person/undo-Recognition', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(reminderData)
    });
    if (!response.ok) throw new Error('撤销互认记录失败');
    return response; // 返回响应对象
}
// 撤销‘不互认’操作
async function revokeRecognitionStatus(reminderData) {
    const response = await fetch('/api/Person/revoke-action', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(reminderData)
    });
    if (!response.ok) throw new Error('撤销‘不互认’操作失败');
    return response; // 返回响应对象
}

// 单条互认操作
async function recognizeReport(event, button) {
    event.stopPropagation();
    try {
        const reportId = button.dataset.reportid;
        highlightAndLoadReport(reportId)
        targetreportId = reportId;
        const hospitalReports = currentReports.filter(r =>
            r.id === reportId
        );
        const RecognitionData = prepareRecognitionData(hospitalReports, currentReportType);
        const response = await PLupdateRemoRecognitionStatus(RecognitionData); 
        // 处理成功结果
        const result = await response.json();
        if (result.success) {
            showToast(`成功互认 ${hospitalReports.length}份报告`);
        } else {
            showToast(`上传互认记录失败 :${result.message}`);
            //throw new Error('上传互认记录失败');
        }
        await loadUserReportList(PatientID, currentReportType);
        return { success: true, message: '操作成功' };
    } catch (error) {
        console.error('互认操作失败:', error);
        showToast('互认操作失败: ' + error.message, 'error');
        return { success: false, error: error.message }; // 明确返回错误信息
    }
}
// 撤销互认操作
async function revokeRecognition(event, button) {
    try {

        const reportId = button.dataset.reportid;
        highlightAndLoadReport(reportId)
        targetreportId = reportId;
        const hospitalReports = currentReports.filter(r =>
            r.id === reportId
        );
        const RecognitionData = prepareRecognitionData(hospitalReports, currentReportType);
        const response = await undoRecognition(RecognitionData);
        // 处理成功结果
        const result = await response.json();
        if (result.success) {
            showToast(`成功撤销 ${hospitalReports.length}份报告`);
        } else {
            showToast(`撤销互认操作失败 :${result.message}`);
            //throw new Error('撤销互认操作失败');
        }
        await loadUserReportList(PatientID, currentReportType);
    } catch (error) {
        console.error('撤销互认操作失败:', error);
        showToast('撤销互认操作失败: ' + error.message, 'error');
        return { success: false, error: error.message }; // 明确返回错误信息
    }
}
// 不互认操作
async function rejectReport(event, button) {
    try {
        event.stopPropagation();
        const reportId = button.dataset.reportid;


        // 显示原因选择弹窗
        const reasonData = await showReasonDialog();
        if (!reasonData) return; // 用户取消了


        highlightAndLoadReport(reportId)
        targetreportId = reportId;
        const hospitalReports = currentReports.filter(r =>
            r.id === reportId
        );
        const RecognitionData = prepareRecognitionData(hospitalReports, currentReportType, reasonData);
        const response = await PLupdateRecognitionStatus(RecognitionData); // 0表示不互认 显示  撤销不互认按钮
        // 处理成功结果
        const result = await response.json();
        if (result.success) {
            showToast(`成功不互认 ${hospitalReports.length}份报告`);
        } else {
            showToast(`不互认操作失败: ${result.message}`);
            //throw new Error(result.message);
        }
        await loadUserReportList(PatientID, currentReportType);
    } catch (error) {
        console.error('撤销互认操作失败:', error);
        showToast('撤销互认操作失败: ' + error.message, 'error');
        return { success: false, error: error.message }; // 明确返回错误信息
    }


}

// 撤销操作,撤销‘不互认’操作接口
async function revokeRejection(event, button) {
    try {
        event.stopPropagation();
        const reportId = button.dataset.reportid; 
        highlightAndLoadReport(reportId)
        targetreportId = reportId;
        const hospitalReports = currentReports.filter(r =>
            r.id === reportId
        );
        const RecognitionData = prepareRecognitionData(hospitalReports, currentReportType);
        const response = await revokeRecognitionStatus(RecognitionData);
        // 处理成功结果
        const result = await response.json();
        if (result.success) {
            showToast(`成功撤销 ${hospitalReports.length}份报告`);
        } else {
            showToast(`撤销不互认操作失败 :${result.message}`);
            //throw new Error('撤销不互认操作失败');
        }
        await loadUserReportList(PatientID, currentReportType);
    } catch (error) {
        console.error('撤销互认操作失败:', error);
        showToast('撤销互认操作失败: ' + error.message, 'error');
        return { success: false, error: error.message }; // 明确返回错误信息
    }
    
}


async function toggleRecognition(event, button) {
    event.stopPropagation();

    const reportId = button.dataset.reportid;
    let patientId = button.dataset.patientid;
    const isRecognized = button.classList.contains('btn-recognized');

    // 获取URL中的GHId
    const urlParams = new URLSearchParams(window.location.search);
    const urlType = urlParams.get('type') || '0';//检验：lab 检查：exam
    const ghId = urlParams.get('GHId') || '0';
    //测试数据固定写死
    //if (patientId == 20816) {
    //    patientId = 4602748
    //}
    if (isRecognized) {
        // 撤销逻辑
        if (confirm('确定要撤销此报告的互认状态吗？')) {
            await updateRecognitionStatus(reportId, patientId, ghId, false, urlType);
            button.textContent = '互认';
            button.classList.remove('btn-recognized');
            button.classList.add('btn-recognize');
        }
    } else {
        // 互认逻辑
        if (confirm('确定要将此报告标记为互认吗？')) {
            const success = await updateRecognitionStatus(reportId, patientId, ghId, true, urlType);
            if (success) { 
                button.textContent = '撤销';
                button.classList.remove('btn-recognize');
                button.classList.add('btn-recognized');
            }
        }
    }
}
// 更新互认状态
async function updateRecognitionStatus(reportId, targetStatus, button, isRecognized) {
    try {
        const urlParams = new URLSearchParams(window.location.search);
        const ghId = urlParams.get('GHId') || '0';
        const urlType = urlParams.get('type') || '0';//检验：lab 检查：exam
        const searchType = urlParams.get('searchType') || '0';
        const patientId = PatientID;
        updateUrlParams({ reportId: reportId });
        //if (PatientID == '20816') {
        //PatientID = '4602748';
        //}
        //使用枚举转换参数类型
        const ActionType = {
            SUBMIT_NON_RECOGNITION: '0', // 上传不互认记录
            UPLOAD_VIEW_RECORD: '1',    // 调阅记录上传
            REVOKE_ACTION: '2',         // 撤销不互认
            BATCH_RECOGNITION: '3',     // 批量互认
            REVOKE_RECOGNITION: '4'     // 撤销互认
        };
        let endpoint = '';
        switch (targetStatus) {
            case 0: endpoint = 'submit-recognition'; czlxValue = ActionType.SUBMIT_NON_RECOGNITION; break; //(上传不互认记录)
            case 1: endpoint = 'upload-view-record'; czlxValue = ActionType.UPLOAD_VIEW_RECORD; break;// 互认接口 3.2.2.5.10.调阅记录上传
            case 2: endpoint = 'revoke-action'; czlxValue = ActionType.REVOKE_ACTION; break;//撤销‘不互认’操作接口
            case 3: endpoint = 'submit-batch-recognition'; czlxValue = ActionType.BATCH_RECOGNITION; break;//批量互认操作接口
            case 4: endpoint = 'undo-Recognition'; czlxValue = ActionType.REVOKE_RECOGNITION; break;//撤销互认
        }

        const response = await fetch(`/api/Person/${endpoint}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                reportId: reportId,
                patientId: patientId,
                ghId: ghId,
                recognise: isRecognized,// 保持与Swagger相同的参数名
                reportType: urlType,
                searchType: searchType,
                CZLX: czlxValue //传入操作类型
            })
        });

        if (!response.ok) throw new Error(await response.text());

        const result = await response.json();
        if (result.success) {
            showToast(getStatusMessage(targetStatus));
            // 刷新当前医院的报告列表
            if (button) {
                const hospitalGroup = button.closest('.hospital-group');
                if (hospitalGroup) {
                    const hospitalName = hospitalGroup.querySelector('.hospital-title').textContent;
                    refreshHospitalReports(hospitalName);
                }
            }
        } else {
            throw new Error(result.message);
        }
        highlightAndLoadReport(reportId)
    } catch (error) {
        console.error('操作失败:', error);
        showToast(`操作失败: ${error.message}`, 'error');
    }
}

// 准备记录数据
// 修改prepareReminderData函数以处理不同数据结构
function prepareRecognitionData(reports, type, reasonData = null) {
    if (!reports || reports.length === 0) return [];
    const urlParams = new URLSearchParams(window.location.search);

    return reports.map(report => {
        const baseData = {
            patientId: PatientID,
            ghId: urlParams.get('GHId') || '0',
            reportId: report.id || report.checkRecNo,
            reportType: type,
            orgCode: report.orgCode,
            orgName: report.orgName || report.checkReportOrgName,
            reportName: type === 'lab' ? report.testProjCategoryName : report.checkProjNameExp,
            reportTime: formatDateTime(type === 'lab' ? report.testReportDate : report.checkReportDate),
            searchType: urlParams.get('searchType'),
            reportNo: type === 'lab' ? report.testReportNo : report.checkReportNo,
            dataCode: report.dataCode,
            idCardValue: report.idCardValue,
            businessNo: report.businessNo,
            idCardTypeCode: report.idCardTypeCode
        };

        // 添加不互认原因数据
        if (reasonData) {
            baseData.reasonCode = reasonData.reasonCode;
            baseData.reasonName = reasonData.reasonName;
            if (reasonData.diagName) {
                baseData.diagName = reasonData.diagName;
            }
        }

        return baseData;
    }).filter(item => item !== null);
}
// 渲染报告列表
function renderReportList(reports, type = 'lab') {
    let container = document.getElementById('reportItems');
    const urlParams = new URLSearchParams(window.location.search);
    //let targetreportId = urlParams.get('reportId');
    // 修复1：先克隆再清空
    const newContainer = container.cloneNode(false); // 只克隆节点，不克隆子元素
    container.parentNode.replaceChild(newContainer, container);
    container = newContainer;

    // 保存当前报告列表
    currentReports = reports;

    // 按医院分组
    const reportsByHospital = groupReportsByHospital(reports);

    // 渲染每个医院分组
    reportsByHospital.forEach((hospitalReports, index) => {
        const hospitalGroup = document.createElement('div');
        hospitalGroup.className = `hospital-group ${index === 0 ? 'expanded' : ''}`;

        hospitalGroup.innerHTML = `
            <div class="hospital-header">
                <div class="hospital-title">${hospitalReports.hospitalName}</div>
                <div class="hospital-count">${hospitalReports.reports.length}份报告</div>
            </div>
            <div class="hospital-reports">
                ${renderHospitalReports(hospitalReports.reports, type)}
                <div class="recognition-actions">
                    <button class="recognition-btn btn-recognize" >全部互认</button>
                    <button class="recognition-btn btn-reject">全部不互认</button>
                </div>
            </div>
        `;

        // 添加点击事件切换展开/折叠  
        const header = hospitalGroup.querySelector('.hospital-header');
        header.addEventListener('click', () => {
            hospitalGroup.classList.toggle('expanded');
        });

        container.appendChild(hospitalGroup);
    });

    // 修复2：统一事件委托（不再嵌套）
    container.addEventListener('click', function (e) {
        // 1. 处理批量操作按钮
        const batchBtn = e.target.closest('.btn-recognize, .btn-reject');
        if (batchBtn) {
            e.stopPropagation();
            const hospitalGroup = e.target.closest('.hospital-group');
            const hospitalName = hospitalGroup.querySelector('.hospital-title').textContent;
            const isRecognize = batchBtn.classList.contains('btn-recognize');
            handleBatchRecognition(hospitalName, isRecognize);
            return; // 阻止后续逻辑
        }

        // 2. 处理报告项点击
        const reportItem = e.target.closest('.report-item');
        if (reportItem) {
            const report = currentReports.filter(r => r.id === reportItem.dataset.reportid); 
            // 查阅模式上传记录
            if (currentMode === 'view') {
                const RecognitionData = prepareRecognitionData(report, currentReportType);
                uploadViewRecord(RecognitionData);
            }

            // 非按钮区域点击加载详情
            if (!e.target.closest('.recognition-btn')) {
                document.querySelectorAll('.report-item').forEach(i => i.classList.remove('active'));
                reportItem.classList.add('active');
                if (report) {
                    loadReportDetails(report[0].testReportNo, report[0].id, currentReportType);
                    updateUrlParams({ reportId: report[0].id, type: currentReportType });
                }
            }
        }
    });
    // 上传调阅记录
    async function uploadViewRecord(reports) {
        try {
            //const report = currentReports.find(r => (r.id === reportId || r.reportId === reportId));
            //if (!report) return;

            //const urlParams = new URLSearchParams(window.location.search);
            //const ghId = urlParams.get('GHId') || '0';

            //// 准备调阅数据
            //const viewData = {
            //    patientId: PatientID,
            //    ghId: ghId,
            //    reportId: report.id || report.reportId,
            //    reportType: report.type || currentReportType,
            //    orgCode: report.orgCode || '',
            //    orgName: report.orgName || '',
            //    reportName: currentReportType === 'lab'
            //        ? report.testProjCategoryName
            //        : report.checkProjNameExp,
            //    reportTime: currentReportType === 'lab'
            //        ? formatDateTime(report.testReportDate)
            //        : formatDateTime(report.checkReportDate),
            //    viewTime: formatDateTime(new Date())
            //};

            // 调用调阅记录上传接口
            const response = await fetch('/api/Person/PLupload-view-record', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(reports)
            });

            if (!response.ok) {
                throw new Error('上传调阅记录失败');
                showToast('上传调阅记录失败');
            } else {
                showToast(`调阅记录上传成功,数量:${reports.length}`);
            }

        } catch (error) {
            console.error('上传调阅记录失败:', error);
        }
    }
    // 默认选中第一个报告
    if (reports.length > 0) {
        const firstReportItem = container.querySelector('.report-item');
        if (firstReportItem) {
            //firstReportItem.classList.add('active');
            // 如果有初始报告ID，加载该报告
            if (targetreportId) {
                highlightAndLoadReport(targetreportId);
            } else {
                // 否则加载第一个报告
                //loadReportDetails(reports[0].testReportNo, reports[0].id, type);
                highlightAndLoadReport(reports[0].id);
            }
            //loadReportDetails(reports[0].testReportNo, reports[0].id, type);
            adjustUIForMode();
        }
    }
}
function updatePatientHeader(report) {
    document.getElementById('patient-name').textContent = report.Name;
    document.getElementById('patient-gender').textContent = report.Gender;
    document.getElementById('patient-age').textContent = calculateAge(report.BirthDate);
    document.getElementById('patient-id').textContent = report.IdCard;
    document.getElementById('patient-address').textContent = '-';
}
function updateReportDetails(report) {
    // 更新报告头部信息
    document.getElementById('detail-hospital').textContent = report.orgName;
    document.getElementById('detail-project').textContent = report.testProjCategoryName;

    // 更新患者详情信息
    document.getElementById('detail-name').textContent = report.patientName;
    document.getElementById('detail-gender').textContent = report.genderName;
    document.getElementById('detail-age').textContent = calculateAge(report.birthDate);
    document.getElementById('detail-medical-no').textContent = report.patientOrgNo;
    document.getElementById('detail-department').textContent = report.testApplyDepartName;
    document.getElementById('detail-bed-no').textContent = report.bedNo || '-';
    document.getElementById('detail-diagnosis').textContent = report.reportClinialDiag || '无';
    document.getElementById('detail-doctor').textContent = report.applDoctName;
    document.getElementById('detail-sample-type').textContent = report.specimenName;
    document.getElementById('detail-report-time').textContent = formatDateTime(report.testReportDate);

    // 更新检验结果表格
    const tbody = document.querySelector('#result-table tbody');
    tbody.innerHTML = '';

    report.testrResIndicates.forEach(item => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${item.testProjNameExp}</td>
            <td class="${item.anomalyCode !== '1' ? 'abnormal' : 'normal'}">${item.testIndexResult}</td>
            <td>${item.testIndexUint || '-'}</td>
            <td>${item.normalRefLimit || '-'}</td>
            <td class="${item.anomalyCode !== '1' ? 'abnormal' : 'normal'}">${item.anomalyName}</td>
        `;
        tbody.appendChild(row);
    });

    // 更新页脚信息
    document.getElementById('test-doctor').textContent = report.testDoctName || '-';
    document.getElementById('audit-doctor').textContent = report.auditDoctName || '-';
    document.getElementById('report-comment').textContent = report.testReportComment || '无';
}
// 统一处理响应
async function handleResponse(response) {
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return await response.json();
}


function renderReportDetails(data, data2, data3, data4) {
    const container = document.getElementById('reportDetails');
    // 如果是微生物报告且有药敏数据，渲染特殊布局
    if (data3 && data3.drugs) {
        renderMicrobialWithDrugReport(container, data, data3);
        return;
    }
    const { tableHeader, tableBody } = getReportContent(data2, data3, data4);
    container.innerHTML = `
        <div class="report-header">
            <h2>${data?.[0]?.orgName || '未知医疗机构'}</h2>
            <h3>${data?.[0]?.testProjCategoryName || '检验报告'}</h3>
            <h3>${getReportTitle(data2, data3, data4) || '检验报告'}</h3>
        </div>
        
        ${getPatientInfoHtml(data)}
        <div class="test-results">
            <h3>检验结果</h3>
            <table class="result-table">
                <thead>
                     ${tableHeader}
                </thead>
                <tbody id="resultItems">
                     ${tableBody}
                </tbody>
            </table>
        </div>
        ${getReportFooterHtml(data)}
        
    `;
}

// 获取报告标题
function getReportTitle(data2, data3, data4) {
    if (data2 && data2.length > 0) return '常规检验报告';
    if (data3 && data3.length > 0) return '微生物培养报告';
    if (data4 && data4.length > 0) return '药敏试验报告';
    return '检验报告';
}
// 获取表格内容和表头
function getReportContent(data2, data3, data4) {
    if (data2 && data2.length > 0) {
        return {
            tableHeader: renderNormalReportHeader(),
            tableBody: renderTestItems(data2)
        };
    }
    if (data3 && data3.length > 0) {
        return {
            tableHeader: renderMicrobialHeader(),
            tableBody: BacterialTestReport(data3)
        };
    }
    if (data4 && data4.length > 0) {
        return {
            tableHeader: renderAntibiogramHeader(),
            tableBody: BacterialAntibiogramReport(data4)
        };
    }
    return {
        tableHeader: '',
        tableBody: noDataRow()
    };
}
function getTestResults(data2, data3, data4) {
    if (data2 && data2.length > 0) {
        return renderNormalReportHeader() + renderTestItems(data2);
    } else if (data3 && data3.length > 0) {
        return renderMicrobialHeader() + BacterialTestReport(data3);
    } else if (data4 && data4.length > 0) {
        return renderAntibiogramHeader() + BacterialAntibiogramReport(data4);
    } else {
        return noDataRow();
    }
}
// 常规检验报告内容渲染
function renderTestItems(items) {
    if (!items || items.length === 0) return noDataRow();

    return items.map(item => `
        <tr>
            <td>${item.testProjNameExp || '-'}</td>
            <td class="${item.anomalyCode !== '1' ? 'abnormal' : 'normal'}">
                ${item.testIndexResult || '-'}
            </td>
            <td>${item.testIndexUint || '-'}</td>
            <td>${item.normalRefLimit || '-'}</td>
            <td class="${item.anomalyCode !== '1' ? 'abnormal' : 'normal'}">
                ${item.anomalyName || (item.anomalyCode === '1' ? '正常' : '异常')}
            </td>
        </tr>
    `).join('');
}
// 微生物培养报告内容渲染
function BacterialTestReport(items) {
    if (!items || items.length === 0) return noDataRow();

    return items.map(item => `
        <tr>
            <td>${item.bacteriaName || '-'}</td>
            <td>${item.colonyCount || '-'}</td>
            <td>${item.incubationCondition || '-'}</td>
            <!--<td class="${item.testResult !== '' ? 'abnormal' : 'normal'}">-->
            <td>
                ${item.testResult||'-'}
            </td>
            <td>
                ${item.bacteriaResultDescription || '-'}
            </td>
        </tr>
    `).join('');
}

// 药敏试验报告内容渲染
//function BacterialAntibiogramReport(items) {
//    if (!items || items.length === 0) return noDataRow();

//    return items.map(item => {
//        const sensitivityClass =
//            item.Sensitivity === 'R' ? 'resistant' :
//                item.Sensitivity === 'I' ? 'intermediate' : 'sensitive';

//        return `
//        <tr class="${sensitivityClass}">
//            <td>${item.drugSusceptibleName || '-'}</td>
//            <td>${item.InspectionMethods || '-'}</td>
//            <td>${item.TestUnit || '-'}</td>
//            <td>
//                ${item.DrugSusceptibilityName||'-'}
//            </td>
//            <td>${item.ExpertRule || '-'}</td>
//        </tr>`;
//    }).join('');
//}
// 药敏试验报告内容渲染
function BacterialAntibiogramReport(items) {
    if (!items || items.length === 0) return noDataRow();

    // 创建字典编码到样式类的映射
    const codeToClass = {
        '1': 'resistant',    // 耐药
        '2': 'sensitive',    // 敏感
        '3': 'contaminated', // 污染
        '4': 'not-done',     // 未做
        '5': 'intermediate', // 中介
        '6': 'negative',     // 阴性
        '7': 'positive'      // 阳性
    };

    // 创建字典编码到中文名称的映射
    const codeToName = {
        '1': '耐药',
        '2': '敏感',
        '3': '污染',
        '4': '未做',
        '5': '中介',
        '6': '阴性',
        '7': '阳性'
    };

    return items.map(item => {
        const sensitivityClass = codeToClass[item.drugSusceptibilityCode] || '';
        const resultName = codeToName[item.drugSusceptibilityCode] || '-';

        return `
        <tr class="${sensitivityClass}">
            <td>${item.drugSusceptibleName || '-'}</td>
            <td>${item.inspectionMethods || '-'}</td>
            <td>${item.testUnit || '-'}</td>
            <td>${item.expertRule || '-'}</td>
            <td>
                ${resultName}
            </td>
        </tr>`;
    }).join('');
}
// 常规检验报告表头
function renderNormalReportHeader() {
    return `
        <thead>
            <tr>
                <th width="25%">项目名称</th>
                <th width="15%">结果</th>
                <th width="15%">单位</th>
                <th width="25%">参考范围</th>
                <th width="20%">状态</th>
            </tr>
        </thead>
    `;
}

// 微生物培养报告表头
function renderMicrobialHeader() {
    return `
        <thead>
            <tr>
                <th width="25%">微生物名称</th>
                <th width="15%">菌落计数</th>
                <th width="20%">检测方法</th>
                <th width="20%">检测结果</th>
                <th width="20%">结果描述</th>
            </tr>
        </thead>
    `;
}

// 药敏试验报告表头
function renderAntibiogramHeader() {
    return `
        <thead>
            <tr>
                <th width="30%">抗生素名称</th>
                <th width="15%">检验方法</th>
                <th width="15%">单位</th>
                <th width="20%">结果说明</th>
                <th width="20%">敏感度</th>
            </tr>
        </thead>
    `;
}

// 无数据行
function noDataRow() {
    return `
        <tr>
            <td colspan="5" style="text-align:center;padding:20px;color:#7f8c8d">
                无检验结果数据
            </td>
        </tr>
    `;
}

//返回报告格式
function getRenderMethod(data) {
    if (data?.Report_testr_res_indicate?.length > 0) {
        return renderTestItems(data.report_testr_res_indicate);
    } else if (data?.Report_TMICROBE_BACTERIA_RES?.length > 0) {
        return BacterialTestReport(data.Report_TMICROBE_BACTERIA_RES);
    } else if (data?.Report_TMICROBE_SUSCEPT_RES?.length > 0) {
        return BacterialTestReport(data.Report_TMICROBE_SUSCEPT_RES);
    }
}
// 高亮并加载指定报告
function highlightAndLoadReport(reportId) {
    // 1. 查找目标报告项
    const item = document.querySelector(`.report-item[data-reportid="${reportId}"]`);

    if (item) {
        // 2. 清除其他报告项的高亮状态
        document.querySelectorAll('.report-item').forEach(i => i.classList.remove('active'));

        // 3. 高亮当前报告项
        item.classList.add('active');

        // 4. 滚动到目标位置（核心新增功能）
        item.scrollIntoView({
            behavior: 'smooth',  // 平滑滚动效果
            block: 'nearest',   // 垂直方向对齐方式
            inline: 'start'      // 水平方向对齐方式
        });

        // 5. 从当前报告列表中查找对应报告
        const report = currentReports.find(r => r.id === reportId);

        // 6. 加载报告详情
        if (report) {
            loadReportDetails(report.testReportNo, report.id, currentReportType);
            updateUrlParams({ reportId, type: currentReportType });
        }
    }
}

// 显示加载状态
function showLoading(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = '<div class="loading-spinner"></div>';
    }
}

// 显示错误信息
function showErrorMessage(elementId, message) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = `
            <div class="error-message">
                <p>${message}</p>
                <button onclick="location.reload()">刷新页面</button>
            </div>
        `;
    }
}

// 辅助函数：计算年龄
function calculateAge(birthDate) {
    if (!birthDate) return null;
    const birth = new Date(birthDate);
    const now = new Date();
    let age = now.getFullYear() - birth.getFullYear();
    if (now.getMonth() < birth.getMonth() ||
        (now.getMonth() === birth.getMonth() && now.getDate() < birth.getDate())) {
        age--;
    }
    return age + '岁';
}

//// 辅助函数：格式化日期时间
//function formatDateTime(dateTime) {
//    if (!dateTime) return null;
//    const date = new Date(dateTime);
//    return `${date.getFullYear()}-${padZero(date.getMonth() + 1)}-${padZero(date.getDate())}
//            ${padZero(date.getHours())}:${padZero(date.getMinutes())}`;
//}
function formatDateTime(dateTime) {
    if (!dateTime) return null;
    const date = new Date(dateTime);
    if (isNaN(date.getTime())) return "Invalid Date"; // 处理无效输入
    return `${date.getFullYear()}-${padZero(date.getMonth() + 1)}-${padZero(date.getDate())} ${padZero(date.getHours())}:${padZero(date.getMinutes())}:${padZero(date.getSeconds())}`;
}
function formatDateTime2(isoString, format = 'YYYY-MM-DD') {
    const date = new Date(isoString);

    // 提取时间组件
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0'); // 月份补零
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const seconds = String(date.getSeconds()).padStart(2, '0');

    // 根据格式替换占位符
    return format
        .replace('YYYY', year)
        .replace('MM', month)
        .replace('DD', day)
        .replace('HH', hours)
        .replace('mm', minutes)
        .replace('ss', seconds);
}
// 辅助函数：补零
function padZero(num) {
    return num < 10 ? `0${num}` : num;
}
//请求函数
async function fetchApiData(endpoint, params = {}) {
    try {
        const url = new URL(`/api/Products/${endpoint}`, window.location.origin);

        // 处理路径参数和查询参数
        Object.entries(params).forEach(([key, value]) => {
            if (value !== undefined) {
                if (url.pathname.includes(`:${key}`)) {
                    url.pathname = url.pathname.replace(`:${key}`, encodeURIComponent(value));
                } else {
                    url.searchParams.append(key, value);
                }
            }
        });

        const response = await fetch(url, {
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });

        // 检查重定向
        if (response.redirected) {
            throw new Error(`请求被重定向到: ${response.url}`);
        }

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || `请求失败: ${response.status}`);
        }

        return response.json();
    } catch (error) {
        console.error(`API请求失败 [${endpoint}]:`, error);
        throw error; // 继续抛出错误供上层处理
    }
}
// 检查数据是否有效（新增辅助函数）
function isValidData(data) {
    return data &&
        (!Array.isArray(data) || data.length > 0) &&
        (!(data instanceof Object) || Object.keys(data).length > 0);
}



// ==================== 封装的专用请求方法 ====================
async function fetchHeaderData(reportId) {
    const url = new URL('/api/Products/detail', window.location.origin);
    url.searchParams.append('reportId', reportId);
    const data = await fetchData(url);
    if (!data) throw new Error('表头数据加载失败');
    return data;
}

async function fetchDetailsData(TestReportNo, reportId) {
    if (!reportId) return null;
    try {
        const url = new URL(`/api/Products/report-details/${TestReportNo}/${reportId}`, window.location.origin);
        return await fetchData(url);
    } catch (error) {
        console.warn('常规检验加载失败:', error);
        return null;
    }
}

async function fetchMicrobialData(TestReportNo, reportId) {
    if (!reportId) return null;
    try {
        const url = new URL(`/api/Products/microbial-culture/${TestReportNo}/${reportId}`, window.location.origin);
        return await fetchData(url);
    } catch (error) {
        console.warn('微生物报告加载失败:', error);
        return null;
    }
}

async function fetchDrugData(TestReportNo, reportId) {
    if (!reportId) return null;
    try {
        const url = new URL(`/api/Products/drug-sensitivity-reports/${TestReportNo}/${reportId}`, window.location.origin);
        return await fetchData(url);
    } catch (error) {
        console.warn('药敏报告加载失败:', error);
        return null;
    }
}

// 基础请求方法（保持不变）
async function fetchData(url) {
    const response = await fetch(url, {
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        }
    });

    if (response.redirected) throw new Error(`请求被重定向到: ${response.url}`);
    if (!response.ok) throw new Error(await response.text() || `请求失败: ${response.status}`);

    return response.json();
}
// 切换报告类型
async function switchReportType(type) {
    if (currentReportType === type) return;
    targetreportId = null;
    currentReportType = type;

    // 更新按钮状态
    document.getElementById('lab-switch').classList.toggle('active', type === 'lab');
    document.getElementById('exam-switch').classList.toggle('active', type === 'exam');

    // 清空当前显示
    document.getElementById('reportItems').innerHTML = '';
    document.getElementById('reportDetails').innerHTML = '<div class="loading-spinner"></div>';

    // 加载新类型的报告列表
    await loadUserReportList(PatientID, type);
}
// 渲染检查报告详情
function renderExamReportDetails(data) {
    const container = document.getElementById('reportDetails');

    container.innerHTML = `
        <div class="report-header">
            <h2>${data[0].orgName || '未知医疗机构'}</h2>
            <h3>${data[0].checkProjNameExp || '检查报告'}</h3>
        </div>
        
        <div class="patient-info">
            <div class="info-row">
                <div class="info-item">
                    <span class="info-label">姓名：</span>
                    <span class="info-value">${data[0].patientName || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">性别：</span>
                    <span class="info-value">${data[0].genderName || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">年龄：</span>
                    <span class="info-value">${calculateAge(data[0].birthDate) || '-'}</span>
                </div>
            </div>
            
            <div class="info-row">
                <div class="info-item">
                    <span class="info-label">病历号：</span>
                    <span class="info-value">${data[0].patNo || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">科室：</span>
                    <span class="info-value">${data[0].wardName || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">检查号：</span>
                    <span class="info-value">${data[0].checkRecNo || '-'}</span>
                </div>
            </div>
            
            <div class="info-row">
                <div class="info-item" style="min-width:100%">
                    <span class="info-label">临床诊断：</span>
                    <span class="info-value">${data[0].reportClinialDiag || '未填写'}</span>
                </div>
            </div>
        </div>
        
        <div class="exam-results">
            <h3>检查所见</h3>
            <div class="exam-findings">${data[0].checkResObj || '无'}</div>
            
            <h3>检查结论</h3>
            <div class="exam-conclusion">${data[0].checkResSub || '无'}</div>
        </div>
        
        <div class="report-footer">
            <div class="footer-row">
                <span><strong>检查医生：</strong> ${data[0].checkDoctName || '-'}</span>
                <span><strong>审核医生：</strong> ${data[0].auditDoctName || '-'}</span>
            </div>
            <div class="footer-row">
                <span><strong>报告时间：</strong> ${formatDateTime(data[0].checkReportDate) || '-'}</span>
                <span><strong>审核时间：</strong> ${formatDateTime(data[0].checkAuditTime) || '-'}</span>
            </div>
        </div>
    `;
}
// 获取检查报告数据
async function fetchExamData(reportId) {
    if (!reportId) return null;
    const url = new URL('/api/Products/detail', window.location.origin);
    url.searchParams.append('reportId', reportId);
    url.searchParams.append('type', 'exam');
    const data = await fetchData(url);
    if (!data) throw new Error('表头数据加载失败');
    return data;
}
// 更新URL参数辅助函数
function updateUrlParams(params) {
    const urlParams = new URLSearchParams(window.location.search);

    // 更新参数
    Object.entries(params).forEach(([key, value]) => {
        if (value) {
            urlParams.set(key, value);
        } else {
            urlParams.delete(key);
        }
    });

    // 更新浏览器URL
    history.replaceState(null, '', `?${urlParams.toString()}`);
}

async function fetchCombinedMicrobialData(TestReportNo, reportId) {
    try {
        const url = new URL(`/api/Products/microbial-combined-report/${TestReportNo}/${reportId}`, window.location.origin);
        const response = await fetch(url);

        if (!response.ok) throw new Error(await response.text());

        const data = await response.json();

        // 处理返回的数据
        if (data.bacteriaResults && data.bacteriaResults.length > 0) {
            // 有微生物培养结果
            if (data.hasDrugResults) {
                // 有药敏结果
                return {
                    header: data.baseInfo,
                    bacteria: data.bacteriaResults,
                    drugs: data.drugResults
                };
            } else {
                // 只有微生物培养结果
                return {
                    header: data.baseInfo,
                    bacteria: data.bacteriaResults,
                    drugs: null
                };
            }
        } else {
            // 只有基本信息
            return {
                header: data.baseInfo,
                bacteria: null,
                drugs: null
            };
        }
    } catch (error) {
        console.error('加载微生物报告失败:', error);
        throw error;
    }
}
// 渲染微生物+药敏报告
function renderMicrobialWithDrugReport(container, headerData, microbialData) {
    container.innerHTML = `
        <div class="report-header">
            <h2>${headerData?.[0]?.orgName || '未知医疗机构'}</h2>
            <h3>${headerData?.[0]?.testProjCategoryName || '微生物检验报告'}</h3>
            <h3>微生物培养及药敏试验报告</h3>
        </div>
        
        ${getPatientInfoHtml(headerData)}
        
        <!-- 微生物培养结果部分 -->
        <div class="microbial-section">
            <div class="section-header">
                <h3><i class="icon-flask"></i> 微生物培养结果</h3>
            </div>
            <table class="result-table">
                ${renderMicrobialHeader()}
                <tbody>
                    ${BacterialTestReport(microbialData.bacteria)}
                </tbody>
            </table>
            
            <div class="microbial-notes">
                <p><strong>培养条件：</strong> ${getIncubationConditions(microbialData.bacteria)}</p>
                <p><strong>备注：</strong> ${getMicrobialNotes(microbialData.bacteria)}</p>
            </div>
        </div>
        
        <!-- 药敏试验结果部分 -->
        <div class="drug-section">
            <div class="section-header">
                <h3><i class="icon-medkit"></i> 药敏试验结果</h3>
                <div class="drug-legend">
                    <span class="legend-item resistant"><span class="legend-color"></span>耐药</span>
                    <span class="legend-item sensitive"><span class="legend-color"></span>敏感</span>
                    <span class="legend-item intermediate"><span class="legend-color"></span>中介</span>
                    <span class="legend-item contaminated"><span class="legend-color"></span>污染</span>
                    <span class="legend-item not-done"><span class="legend-color"></span>未做</span>
                    <span class="legend-item negative"><span class="legend-color"></span>阴性</span>
                    <span class="legend-item positive"><span class="legend-color"></span>阳性</span>
                </div>
            </div>
            <table class="result-table drug-table">
                ${renderAntibiogramHeader()}
                <tbody>
                    ${BacterialAntibiogramReport(microbialData.drugs)}
                </tbody>
            </table>
            
            <div class="drug-notes">
                <!--<p><strong>检测方法：</strong> ${getDrugTestMethod(microbialData.drugs)}</p>-->
                <p><strong>备注：</strong> 本结果仅供参考，临床用药请结合患者具体情况</p>
            </div>
        </div>
        
        ${getReportFooterHtml(headerData)}
    `;
}
// 辅助函数：获取患者信息HTML
function getPatientInfoHtml(data) {
    return `
        <div class="patient-info">
            <div class="info-row">
                <div class="info-item">
                    <span class="info-label">姓名：</span>
                    <span class="info-value">${data?.[0]?.patientName || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">性别：</span>
                    <span class="info-value">${data?.[0]?.genderName || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">年龄：</span>
                    <span class="info-value">${calculateAge(data?.[0]?.birthDate) || '-'}</span>
                </div>
            </div>
            
            <div class="info-row">
                <div class="info-item">
                    <span class="info-label">病历号：</span>
                    <span class="info-value">${data?.[0]?.patNo || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">科室：</span>
                    <span class="info-value">${data?.[0]?.testApplyDepartNameExp || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">床号：</span>
                    <span class="info-value">${data?.[0]?.bedNo || '-'}</span>
                </div>
            </div>
            
            <div class="info-row">
                <div class="info-item" style="min-width:100%">
                    <span class="info-label">临床诊断：</span>
                    <span class="info-value">${data?.[0]?.reportClinialDiag || '未填写'}</span>
                </div>
            </div>
        </div>
    `;
}
// 辅助函数：获取报告页脚HTML
function getReportFooterHtml(data) {
    return `
        <div class="report-footer">
            <div class="footer-row">
                <span><strong>检验医生：</strong> ${data?.[0]?.testDoctName || '-'}</span>
                <span><strong>审核医生：</strong> ${data?.[0]?.auditDoctName || '-'}</span>
            </div>
            <div class="footer-row">
                <span><strong>报告时间：</strong> ${formatDateTime(data?.[0]?.testReportDate) || '-'}</span>
                <span><strong>采样时间：</strong> ${formatDateTime(data?.[0]?.sampleTime) || '-'}</span>
            </div>
        </div>
    `;
}
// 辅助函数：获取培养条件
function getIncubationConditions(bacteriaData) {
    if (!bacteriaData || bacteriaData.length === 0) return '-';
    const conditions = [...new Set(bacteriaData.map(item => item.incubationCondition))];
    return conditions.filter(Boolean).join('，') || '-';
}
// 辅助函数：获取微生物备注
function getMicrobialNotes(bacteriaData) {
    // 这里可以根据实际业务逻辑添加更多备注信息
    return bacteriaData && bacteriaData.length > 0
        ? bacteriaData[0].foundWay
        : '无特殊备注';
}
// 辅助函数：获取药敏检测方法
function getDrugTestMethod(drugData) {
    if (!drugData || drugData.length === 0) return '-';
    // 假设第一个药物的检测方法代表全部
    return drugData[0].InspectionMethods; //;
}
// 按医院分组报告
function groupReportsByHospital(reports) {
    const hospitalMap = {};

    reports.forEach(report => {
        const hospitalName = report.orgName || '未知医院';
        if (!hospitalMap[hospitalName]) {
            hospitalMap[hospitalName] = [];
        }
        hospitalMap[hospitalName].push(report);
    });

    return Object.entries(hospitalMap).map(([hospitalName, reports]) => ({
        hospitalName,
        reports
    }));
}

// 渲染医院报告列表
function renderHospitalReports(reports, type) {
    return reports.map(report => {
        const status = report.isRecognized;
        let buttons = '';
        if (currentMode === 'view') {

        } else {
            // 状态判断
            if (status === '1') {
                // 已互认状态 - 显示撤销互认按钮
                buttons = `
                <div class="report-actions">
                    <button class="recognition-btn btn-revoke" 
                            data-reportid="${report.id || report.reportId}"
                            onclick="revokeRecognition(event, this)">
                        撤销互认
                    </button>
                </div>
            `;
            }
            else if (status === '0') {
                // 不互认状态 - 显示撤销按钮
                buttons = `
                <div class="report-actions">
                    <button class="recognition-btn btn-revoke" 
                            data-reportid="${report.id || report.reportId}"
                            onclick="revokeRejection(event, this)">
                        撤销不互认
                    </button>
                </div>
            `;
            }
            else {
                // 未处理状态 - 显示互认和不互认按钮
                buttons = `
                <div class="report-actions">
                    <button class="recognition-btn btn-recognize" 
                            data-reportid="${report.id || report.reportId}"
                            onclick="recognizeReport(event, this)">
                        互认
                    </button>
                    <button class="recognition-btn btn-reject" 
                            data-reportid="${report.id || report.reportId}"
                            onclick="rejectReport(event, this)">
                        不互认
                    </button>
                </div>
            `;
            }
        } 

        return `
            <div class="report-item" data-reportid="${report.id || report.reportId}">
                <div class="report-item-content">
                    <div class="hospital">${report.orgName}</div>
                    <div class="project">${type === 'lab' ? report.testProjCategoryName : report.checkProjNameExp}</div>
                    <div class="time">${formatDateTime(type === 'lab' ? report.testReportDate : report.checkReportDate)}</div>
                </div>
                ${buttons}
            </div>
        `;
    }).join('');
}





// 刷新指定医院的报告列表
async function refreshHospitalReports(hospitalName) {
    await loadUserReportList(PatientID, currentReportType);
}

// 状态消息映射
function getStatusMessage(status) {
    const messages = {
        0: '报告已标记为不互认',
        1: '报告已成功互认',
        2: '操作已撤销'
    };
    return messages[status] || '操作成功';
}

// 全局定义 showToast 函数
function showToast(message, type = 'info', duration = 10000) {
    // 创建容器（如果不存在）
    let container = document.getElementById('left-toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'left-toast-container';
        container.className = 'left-toast-container';
        document.body.appendChild(container);
    }

    // 创建通知元素
    const toast = document.createElement('div');
    toast.className = `left-toast toast-${type}`;
    const toastId = 'toast-' + Date.now();
    toast.id = toastId;
    toast.innerHTML = `
        <div class="toast-message">${message}</div>
        <div class="toast-close">&times;</div>
        <div class="toast-progress"></div>
    `;

    // 添加到容器
    container.appendChild(toast);

    // 强制重绘
    void toast.offsetWidth;

    // 显示动画
    toast.classList.add('show');

    // 为每个通知创建独立的进度条样式
    const progressStyle = document.createElement('style');
    progressStyle.textContent = `
        #${toastId} .toast-progress::after {
            content: '';
            position: absolute;
            left: 0;
            top: 0;
            height: 100%;
            width: 100%;
            background: ${getProgressColor(type)};
            animation: progress-${toastId} ${duration}ms linear forwards;
            transform-origin: left center;
        }
        
        @keyframes progress-${toastId} {
            0% { transform: scaleX(1); }
            100% { transform: scaleX(0); }
        }
    `;
    document.head.appendChild(progressStyle);

    // 动画结束时清理资源
    const progressBar = toast.querySelector('.toast-progress');
    if (progressBar) {
        progressBar.addEventListener('animationend', () => {
            progressStyle.remove();
        }, { once: true });
    }

    // 自动消失定时器
    const timer = setTimeout(() => {
        hideToast(toast);
        progressStyle.remove();
    }, duration);

    // 点击关闭
    toast.querySelector('.toast-close').addEventListener('click', () => {
        clearTimeout(timer);
        hideToast(toast);
        progressStyle.remove();
    });

    function hideToast(toastElement) {
        toastElement.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
        toastElement.style.opacity = '0';
        toastElement.style.transform = 'translateX(-120%)';
        setTimeout(() => {
            toastElement.remove();
            if (container.children.length === 0) {
                container.remove();
            }
        }, 300);
    }
}

// 获取进度条颜色
function getProgressColor(type) {
    const colors = {
        info: '#3498db',
        success: '#27ae60',
        warning: '#f39c12',
        error: '#e74c3c'
    };
    return colors[type] || colors.info;
}


// 新增renderNoData函数
function renderNoData(headerData) {
    const container = document.getElementById('reportDetails');
    container.innerHTML = `
        <div class="report-header">
            <h2>${headerData?.[0]?.orgName || '未知医疗机构'}</h2>
            <h3>${headerData?.[0]?.testProjCategoryName || '检验报告'}</h3>
        </div>
        
        ${getPatientInfoHtml(headerData)}
        
        <div class="no-data-message">
            <p>无检验结果数据</p>
        </div>
        
        ${getReportFooterHtml(headerData)}
    `;
}


// 原因选项数据
const REASON_OPTIONS = [
    { code: "01", name: "因病情变化，检查检验结果与患者临床表现、疾病诊断不符，难以满足临床诊疗需求的" },
    { code: "02", name: "检查检验结果在疾病发展演变过程中变化较快的" },
    { code: "03", name: "检查检验项目对于疾病诊疗意义重大的（如手术、输血等重大医疗措施前）" },
    { code: "04", name: "患者病情处于急诊、急救、重型、危重型等紧急状态下的" },
    { code: "05", name: "涉及司法、伤残、病退、劳动仲裁等鉴定的" },
    { code: "06", name: "国家规定需要相应资质的检测机构的检查检验项目" },
    { code: "07", name: "诊治医师认为有必要进行再次检查的" },
    { code: "08", name: "其他情形确需复查的" }, 
    { code: "99", name: "由于疾病诊断或病情发展监测需要进行连续性检查/检验" }
];

// 显示原因选择弹窗
// 修改后的 showReasonDialog 函数
function showReasonDialog() {
    return new Promise((resolve) => {
        // 创建弹窗元素
        const dialog = document.createElement('div');
        dialog.className = 'reason-dialog';
        dialog.innerHTML = `
            <h3>请选择不互认原因</h3>
            <div class="reason-options">
                ${REASON_OPTIONS.map(option => `
                    <div class="reason-option">
                        <input type="radio" id="reason-${option.code}" name="reason" value="${option.code}">
                        <label for="reason-${option.code}">${option.code} - ${option.name}</label>
                    </div>
                `).join('')}
                <div class="disease-input">
                    <input type="text" id="disease-name" placeholder="请输入疾病名称" required>
                    <div class="error-message" style="color: #e74c3c; font-size: 12px; margin-top: 5px; display: none;">请填写疾病名称</div>
                </div>
            </div>
            <div class="dialog-buttons">
                <button class="cancel-btn">取消</button>
                <button class="confirm-btn">确定</button>
            </div>
        `;

        // 创建遮罩层
        const overlay = document.createElement('div');
        overlay.className = 'dialog-overlay';

        // 添加到body
        document.body.appendChild(overlay);
        document.body.appendChild(dialog);

        // 获取DOM元素
        const diseaseInput = dialog.querySelector('.disease-input');
        const diseaseNameInput = dialog.querySelector('#disease-name');
        const errorMessage = dialog.querySelector('.error-message');
        const confirmBtn = dialog.querySelector('.confirm-btn');

        // 事件监听
        const radioInputs = dialog.querySelectorAll('input[type="radio"]');
        radioInputs.forEach(input => {
            input.addEventListener('change', (e) => {
                diseaseInput.style.display = e.target.value === '99' ? 'block' : 'none';
                errorMessage.style.display = 'none'; // 隐藏错误提示
            });
        });

        dialog.querySelector('.cancel-btn').addEventListener('click', () => {
            document.body.removeChild(overlay);
            document.body.removeChild(dialog);
            resolve(null); // 取消操作
        });

        confirmBtn.addEventListener('click', () => {
            const selected = dialog.querySelector('input[name="reason"]:checked');
            if (!selected) {
                showToast('请选择不互认原因', 'error');
                return;
            }

            const reasonCode = selected.value;
            const reasonName = REASON_OPTIONS.find(o => o.code === reasonCode).name;
            const diagName = reasonCode === '99'
                ? diseaseNameInput.value.trim()
                : '';

            // 新增验证：如果选择99但未填写疾病名称
            if (reasonCode === '99' && !diagName) {
                errorMessage.style.display = 'block';
                diseaseNameInput.focus();
                return;
            }

            document.body.removeChild(overlay);
            document.body.removeChild(dialog);

            resolve({ reasonCode, reasonName, diagName });
        });

        // 添加疾病名称输入的键盘事件监听
        diseaseNameInput.addEventListener('keyup', (e) => {
            if (e.key === 'Enter') {
                confirmBtn.click();
            }
        });
    });
}