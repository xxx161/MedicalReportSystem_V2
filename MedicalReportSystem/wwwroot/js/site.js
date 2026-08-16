// 全局变量
let currentReports = [];
let currentUserId = null;
let PatientID = null;//病人ID
let IdCard = null;//
let currentMode = 'recognize'; // 'recognize'互认模式 或 'view'查阅模式
let currentReportType = 'lab'; // 'lab'检验或'exam'检查，表示当前显示的报告类型
let searchType = '1'; //1：接诊历史提醒2：开单重复提醒
let targetreportId = null;
let doctorId = null;//医生ID
let matchedReports = []; // 存储匹配的报告
let detailsDataReports = [];//存储明细报告
let PatientData = [];//存储用户信息
let DoctorName = '';//医生姓名
let RegistrationNo = '';//挂号单号
let RegistrationID = '';//挂号ID
let OutpatientNo = '';//门诊号
let dataCode = '';
let microbiaMark = '';//微生物标记
// 页面加载初始化
document.addEventListener('DOMContentLoaded', async function () {
    const urlParams = new URLSearchParams(window.location.search);
    PatientID = urlParams.get('PatientID'); // 获取地址栏传入patientId(病人ID)
    IdCard = urlParams.get('idCard'); // 获取地址栏传入IdCard(IdCardID)
    doctorId = urlParams.get('doctorId');
    feeSource = urlParams.get('feeSource');
    const initialReportId = urlParams.get('reportId');
    const initialReportType = urlParams.get('type') || 'lab'; // 默认检验报告
    const mode = urlParams.get('mode') || 'recognize'; // 获取模式参数
    dataCode = urlParams.get('dataCode'); // 获取地址栏传入dataCode
    RegistrationID = urlParams.get('GHId');


    const fileId = urlParams.get('fileId'); // 新的参数名
    const sessionId = urlParams.get('sessionId'); // 保持向后兼容

    let encryptedJson = null;
    let dataSource = '';

    // 优先使用文件ID
    if (fileId) {
        dataSource = 'file';
        try {
            console.log('使用文件存储方案，fileId:', fileId);
            const fileData = await getJsonFromFile(fileId);
            if (fileData && fileData.success && fileData.jsonData) {
                encryptedJson = fileData.jsonData;
                console.log('从文件获取JSON数据成功');
            }
        } catch (error) {
            console.error('获取文件数据失败:', error);
        }
    }

    // 处理JSON数据...
    if (encryptedJson) {
        try {
            const decodedJson = decodeBase64(encryptedJson);
            const jsonData = JSON.parse(decodedJson);

            window.reportContext = {
                ...window.reportContext,
                inputData: jsonData
            };

            console.log(`解析的JSON数据（来源: ${dataSource}）:`, jsonData);
        } catch (e) {
            console.error('JSON参数解析失败:', e);
        }
    }
    // 回退到URL中的JSON参数
    //else if (jsonParam) {
    //    loadDataFromUrl(jsonParam, params);
    //}
    //// 都没有的话，从API获取
    //else {
    //    loadDataFromApi(params);
    //}
    // 新增：处理加密的 JSON 参数
    //const encryptedJson = urlParams.get('json');
    //if (encryptedJson) {
    //    try {
    //        // Base64 解码
    //        const decodedJson = atob(encryptedJson.replace(/-/g, '+').replace(/_/g, '/'));
    //        const jsonData = JSON.parse(decodedJson);

    //        // 将 JSON 数据存入全局变量供后续使用
    //        window.reportContext = {
    //            ...window.reportContext,
    //            inputData: jsonData
    //        };

    //        console.log('解析的JSON数据:', jsonData);
    //    } catch (e) {
    //        console.error('JSON参数解析失败:', e);
    //    }
    //}//window.reportContext?.inputData?.input?.req_info?.[0]
    try {
        const result = await QueryPatientInfo(RegistrationID);
        if (result.success) {
            console.log("病人信息:", result.data);
            DoctorName = result.data.doctorName
            RegistrationNo = result.data.registrationNo
            OutpatientNo = result.data.outpatientNo
            // 返回的数据结构：
            // {
            //   DoctorName: "医生姓名",
            //   RegistrationNo: "挂号单号", 
            //   PatientId: "病人ID",
            //   OutpatientNo: "门诊号",
            //   Exists: true
            // }
        } else {
            console.error("查询失败:", result.Message);
        }
    } catch (error) {
        console.error("发生错误:", error);
    }
    //const encryptedJson = urlParams.get('json');
    //if (encryptedJson) {
    //    try {
    //        const decodedJson = decodeBase64(encryptedJson);
    //        const jsonData = JSON.parse(decodedJson);

    //        window.reportContext = {
    //            ...window.reportContext,
    //            inputData: jsonData
    //        };

    //        console.log('解析的JSON数据:', jsonData);
    //    } catch (e) {
    //        console.error('JSON参数解析失败:', e);
    //    }
    //}


    // 新增：初始化提醒系统
    initReminderSystem();

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
    await loadUserReportList(PatientID, initialReportType, IdCard, dataCode);
    // 根据模式调整UI
    adjustUIForMode();
    // 如果是查阅模式，上传提醒记录
    if (currentMode === 'view') {
        await uploadReminderRecords();
    }
    if (currentMode === 'recognize' && window.reportContext?.inputData) {
        uploadReminderRecords_recognize();
    }
    // 如果有初始报告ID，加载该报告
    //if (initialReportId) {
    //    highlightAndLoadReport(initialReportId);
    //    //const hospitalReports = currentReports.filter(r =>
    //    //r.id === initialReportId
    //    //);
    //    //loadReportDetails(hospitalReports[0].testReportNo, initialReportId, initialReportType)
    //} else if (currentReports.length > 0) {
    //    // 否则加载第一个报告
    //    const firstReport = currentReports[0];
    //    loadReportDetails(firstReport.testReportNo, firstReport.id, initialReportType);
    //}
});

 
// 从文件获取JSON数据的辅助函数
async function getJsonFromFile(fileId) {
    try {
        const url = `/api/reports/get-data?fileId=${encodeURIComponent(fileId)}`;
        console.log('从文件获取数据，fileId:', fileId);

        const response = await fetch(url);

        if (!response.ok) {
            throw new Error(`HTTP错误: ${response.status}`);
        }

        const result = await response.json();

        if (result.success && result.jsonData) {
            console.log('从文件获取数据成功');
            return result;
        } else {
            throw new Error(result.Message || '获取数据失败');
        }
    } catch (error) {
        console.error('获取文件数据失败:', error);
        throw error;
    }
}
// 改进的Base64解码函数（支持UTF-8）
function decodeBase64(base64) {
    // 首先替换URL安全的Base64字符
    const safeBase64 = base64.replace(/-/g, '+').replace(/_/g, '/');

    // 解码为二进制字符串
    const binaryString = atob(safeBase64);

    // 转换为字节数组
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }

    // 使用UTF-8解码
    return new TextDecoder('utf-8').decode(bytes);
}
// 根据模式调整UI
function adjustUIForMode() {
    const recognitionButtons = document.querySelectorAll('.recognition-btn, .recognition-actions');
    if (currentMode === 'view') {
        updateUrlParams({ searchType: '1' })
        // 隐藏所有互认相关按钮
        // 在查阅模式下，只隐藏互认相关按钮，保留查阅相关按钮
        recognitionButtons.forEach(btn => {
            // 隐藏互认/不互认按钮
            if (btn.classList.contains('btn-recognize') ||
                btn.classList.contains('btn-reject') ||
                btn.classList.contains('btn-revoke')) {
                btn.style.display = 'none';
            }
            // 显示已查阅按钮
            else if (btn.classList.contains('btn-viewed')) {
                btn.style.display = '';
            }
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
async function QueryPatientInfo(GHID) {
    // 1. 构建查询参数
    const params = new URLSearchParams();
    params.append('GHID', GHID);  // 必填参数：挂号ID

    // 2. 拼接完整URL（带查询参数）
    const url = `/api/Person/query-patient-info?${params.toString()}`;

    try {
        // 3. 发送GET请求
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        // 4. 检查响应状态
        if (!response.ok) {
            throw new Error(`请求失败，状态码：${response.status}`);
        }

        // 5. 解析并返回JSON数据
        return await response.json();
    } catch (error) {
        // 6. 统一错误处理
        console.error('查询病人基本信息时出错:', error);
        throw new Error(`查询失败: ${error.message}`);
    }
}
// 上传提醒记录（同时处理检验和检查数据）
async function uploadReminderRecords() {
    try {
        const urlParams = new URLSearchParams(window.location.search);
        const ghId = urlParams.get('GHId') || '0';

        // 1. 获取检验数据
        const labReports = await fetchReportsReminder(IdCard, 'lab');
        console.log('检验报告数据:', labReports);
        // 2. 获取检查数据
        const examReports = await fetchReportsReminder(IdCard, 'exam');
        console.log('检查报告数据:', examReports);
        // 3. 合并数据并准备提醒记录
        const reminderData = [
            ...prepareReminderData(labReports[0].reports_TEST_REC, 'lab'),
            ...prepareReminderData(examReports[0].report_CHECK_REC_oracle, 'exam')
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
        const responseResult = await response.json();
        console.log('接口返回结果:', responseResult);
        if (!response.ok) {
            throw new Error(responseResult.message || `HTTP错误: ${response.status} ${response.statusText}`);
        }
        if (!responseResult.success) {
            showToast('提醒记录上传失败:(' + responseResult.code + ')' + responseResult.message)
            return;
        }
        //const result = await response.json();
        if (responseResult.success) {
            showToast(`提醒记录上传成功，共${reminderData.length}条`);
        } else {
            throw new Error(result.message);
        }
    } catch (error) {
        console.error('上传提醒记录失败:', error);
    }
}
// 上传提醒记录(互认提醒)（同时处理检验和检查数据）
async function uploadReminderRecords_recognize() {
    try {
        const urlParams = new URLSearchParams(window.location.search);
        const ghId = urlParams.get('GHId') || '0';
        const reminderDataReports = []

        // 获取输入数据
        const inputData = window.reportContext?.inputData;

        console.log('完整的 inputData 结构:', inputData); // 调试信息

        // 检查数据结构是否正确
        if (!inputData || typeof inputData !== 'object') {
            console.log('输入数据不是对象格式，跳过上传提醒记录');
            return;
        }

        // 确保 input 存在并且有 req_info 数组
        if (!inputData.input || !inputData.input.req_info || !Array.isArray(inputData.input.req_info)) {
            console.log('input.req_info 不存在或不是数组，跳过上传提醒记录');
            return;
        }

        const reqInfoArray = inputData.input.req_info;
        console.log('reqInfoArray 内容:', reqInfoArray); // 调试信息

        // 如果没有输入数据，直接返回
        if (reqInfoArray.length === 0) {
            console.log('req_info 数组为空，跳过上传提醒记录');
            return;
        }

        // 1. 获取检验数据
        const labReports = await fetchReports(IdCard, 'lab', dataCode);
        console.log('检验报告数据:', labReports);

        // 2. 获取检查数据
        const examReports = await fetchReports(IdCard, 'exam', dataCode);
        console.log('检查报告数据:', examReports);

        // 3. 合并数据并准备提醒记录
        const reminderData = [
            ...prepareReminderData(labReports[0].reports_TEST_REC, 'lab'),
            ...prepareReminderData(examReports[0].report_CHECK_REC_oracle, 'exam')
        ];

        console.log('reminderData 内容:', reminderData); // 调试信息

        // 遍历所有报告和输入数据，找到匹配项
        for (const report of reminderData) {
            const reportDataCode = report.dataCode ? report.dataCode.toString() : '';

            if (!reportDataCode) {
                console.log('report.dataCode为空，跳过:', report.reportId);
                continue;
            }

            // 遍历 req_info 数组
            for (const inputItem of reqInfoArray) {
                const inputDataCode = inputItem.dataCode ? inputItem.dataCode.toString() : '';

                if (!inputDataCode) {
                    console.log('inputItem.dataCode为空，跳过');
                    continue;
                }

                console.log('比较 - report.dataCode:', reportDataCode, 'inputItem.dataCode:', inputDataCode);

                //  直接使用 includes 判断包含关系
                // 判断 inputDataCode（逗号分隔的字符串）是否包含 reportDataCode
                if (inputDataCode.includes(reportDataCode)) {
                    console.log('✅ 找到匹配项:', reportDataCode, '包含在', inputDataCode);

                    // 获取查阅状态
                    const status = await fetchRecognitionStatus(PatientID, report.reportId, doctorId);

                    // 如果VIEW_STATUS返回1，跳过此条数据
                    if (status.view_STATUS === '1') {
                        console.log('报告已被查阅，跳过:', reportDataCode);
                        continue;
                    }

                    // 添加匹配的报告
                    reminderDataReports.push({
                        ...report,
                        matchedOrderId: inputItem.orderId,
                        reportType: currentReportType,
                        recognitionStatus: status,
                        matchMethod: 'includes'
                    });
                }
            }
        }

        console.log('合并后的提醒记录数据:', reminderDataReports);
        if (reminderDataReports.length === 0) {
            console.log('没有需要上传的提醒记录');
            return;
        }

        // 4. 调用提醒记录上传接口
        const response = await fetch('/api/Person/upload-reminder-records', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(reminderDataReports)
        });

        if (!response.ok) {
            throw new Error('上传提醒记录失败');
        }

        const result = await response.json();
        if (result.success) {
            showToast(`提醒记录上传成功，共${reminderDataReports.length}条`);
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
        const labReports = await fetchReports(IdCard, 'lab');
        console.log('检验报告数据:', labReports);
        // 2. 获取检查数据
        const examReports = await fetchReports(IdCard, 'exam');
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
async function fetchReports(IdCard, type, dataCode = '') {
    try {
        const listUrl = new URL('/api/Person/report-headers-oracle', window.location.origin);
        listUrl.searchParams.append('userId', IdCard);
        listUrl.searchParams.append('dataCode', dataCode);
        listUrl.searchParams.append('type', type);

        const response = await fetch(listUrl);
        if (!response.ok) throw new Error(`获取${type === 'lab' ? '检验' : '检查'}列表失败`);

        return await response.json();
    } catch (error) {
        console.error(`获取${type === 'lab' ? '检验' : '检查'}列表失败:`, error);
        return [];
    }
}// 获取报告列表(提醒记录)
async function fetchReportsReminder(IdCard, type, reportId = '', customDays = null) {
    try {
        const listUrl = new URL('/api/Person/report-headers-oracleReminder', window.location.origin);
        listUrl.searchParams.append('userId', IdCard);
        listUrl.searchParams.append('type', type);
        listUrl.searchParams.append('reportId', reportId);
        // 如果传递了自定义天数，则添加到查询参数
        if (customDays !== null && customDays !== undefined) {
            listUrl.searchParams.append('days', customDays);
        }

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


    // 获取正确的 inputData 数组
    let inputDataArray = [];
    try {
        if (window.reportContext?.inputData?.input?.req_info) {
            inputDataArray = window.reportContext.inputData.input.req_info;
            console.log('获取到的 inputData 数组:', inputDataArray);
        }
    } catch (e) {
        console.warn('获取 inputData 失败:', e);
    }

    return reports.map(report => {
        // 处理检查数据
        // 根据dataCode匹配orderId
        let orderId = "";
        if (currentMode ==="recognize") {
            // 只有在inputDataArray有数据时才进行匹配
            if (inputDataArray.length > 0) {
                const matchedOrder = inputDataArray.find(item =>
                    item && item.dataCode && report.dataCode &&
                    item.dataCode.toString() === report.dataCode.toString()
                );

                if (matchedOrder) {
                    console.log('找到匹配的报告:', matchedOrder.orderId, '对应报告:', report.dataCode);
                    orderId = matchedOrder.orderId ? matchedOrder.orderId.toString() : "";
                }
            }
        }
        
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
                businessNo: report.businessNo,
                doctorId: doctorId,
                orderId: orderId
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
                businessNo: report.businessNo,
                doctorId: doctorId,
                orderId: orderId
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
async function loadUserReportList(userId, type = 'lab', IdCard = '', dataCode='') {
    try {

        // 从全局变量获取输入数据（JSON）

        showLoading('reportItems');
        // 根据 currentMode 选择不同的URL
        let listUrl;
        if (currentMode === "recognize") {
            // 互认模式 
            listUrl = new URL('/api/Person/report-headers-oracle', window.location.origin);
            console.log(`[loadUserReportList] 使用互认模式URL: ${listUrl.pathname}`);
        } else {
            // 查阅模式- 使用提醒记录URL
            listUrl = new URL('/api/Person/report-headers-oracleReminder', window.location.origin);
            console.log(`[loadUserReportList] 使用提醒记录模式URL: ${listUrl.pathname}`);
        }

        // 添加查询参数
        listUrl.searchParams.append('userId', IdCard);
        listUrl.searchParams.append('type', type);
        listUrl.searchParams.append('dataCode', dataCode);
        const inputData = window.reportContext?.inputData?.input?.req_info?.[0];
        if (inputData?.dataCode) {
            listUrl.searchParams.append('dataCode', inputData.dataCode);
        }

        const listResponse = await fetch(listUrl);
        if (!listResponse.ok) throw new Error('列表加载失败');

        let responseData = await listResponse.json();
        console.log('API返回数据:', responseData);

        // 确保我们处理的是数组
        const reports = Array.isArray(responseData) ? responseData : [responseData];

        // 处理每个报告
        const processedReports = await Promise.all(reports.map(async report => {
            // 处理检查报告（exam类型）
            //if (report.report_CHECK_REC_oracle) {
            //    // 转换为支持多条的格式：{0: {...}, 1: {...}, ...}
            //    const checkRecs = Array.isArray(report.report_CHECK_REC_oracle)
            //        ? report.report_CHECK_REC_oracle
            //        : [report.report_CHECK_REC_oracle];

            //    // 转换为图片结构，但支持多个索引
            //    const processedCheckRecs = {};
            //    await Promise.all(checkRecs.map(async (rec, index) => {
            //        processedCheckRecs[index] = {
            //            ...rec,
            //            isRecognized: await fetchRecognitionStatus(userId, rec.id)
            //        };
            //    }));

            //    report.report_CHECK_REC_oracle = processedCheckRecs;
            //}
            if (report.report_CHECK_REC_oracle) {
                report.report_CHECK_REC_oracle = await Promise.all(
                    report.report_CHECK_REC_oracle.map(async subReport => {
                        const status = await fetchRecognitionStatus(userId, subReport.id, doctorId);
                        return {
                            ...subReport,
                            isRecognized: status.isRecognized,
                            searchType: status.searchType, // 添加SearchType到返回对象
                            view_STATUS: status.view_STATUS,
                            quote_STATUS: status.quote_STATUS
                        };
                    })
                );
            }
            // 处理检验报告（lab类型）保持不变
            if (report.reports_TEST_REC) {
                report.reports_TEST_REC = await Promise.all(
                    report.reports_TEST_REC.map(async subReport => {
                        const status = await fetchRecognitionStatus(userId, subReport.id, doctorId);
                        return {
                            ...subReport,
                            isRecognized: status.isRecognized,
                            searchType: status.searchType, // 添加SearchType到返回对象
                            view_STATUS: status.view_STATUS,
                            quote_STATUS: status.quote_STATUS
                        };
                    })
                );
            }

            return report;
        }));

        // 设置当前报告
        currentReports = type === 'lab'
            ? processedReports[0]?.reports_TEST_REC || []
            : processedReports[0]?.report_CHECK_REC_oracle || [];

        renderReportList(currentReports, type);
        updateUrlParams({ type });

        // 处理完成后查找匹配的报告
        findMatchingReports();

    } catch (error) {
        console.error('加载失败:', error);
        showErrorMessage('reportItems', `无法加载${type === 'lab' ? '检验' : '检查'}列表：${error.message}`);
    }
}

// 提取获取状态的公共方法
async function fetchRecognitionStatus(userId, reportId, doctorId) {
    try {
        const response = await fetch(`/api/Person/recognition-status/${userId}/${reportId}/${doctorId}`);
        if (response.ok) {
            const data = await response.json();
            const dataArray = Object.entries(data);
            console.log("Data 对象转数组:", dataArray);
            return {
                isRecognized: data.isRecognized, // 互认状态 0未互认 1已互认
                externalCode: data.externalCode,
                externalMsg: data.externalMsg,
                searchType: data.searchType, // 注意大小写转换
                view_STATUS: data.vieW_STATUS,
                quote_STATUS: data.quote_STATUS
            };
        }
        return {
            isRecognized: false,
            externalCode: null,
            externalMsg: null,
            searchType: null,
            view_STATUS: false,
            quote_STATUS: null
        };
    } catch (e) {
        console.error(`获取报告 ${reportId} 状态失败:`, e);
        return false;
    }
}
// 加载检验详情（严格优先级控制）
async function loadReportDetails(businessNo, reportId, type) {
    try {
        showLoading('reportDetails');
        if (type === 'lab') {
            // 1. 必须加载表头数据fetchReportsReminder
            let headerData;

            // 根据 currentMode 选择不同的数据源
            if (currentMode === "recognize") {
                console.log(`[loadReportDetails] 互认模式，使用原fetchHeaderData`);
                // 互认模式：使用原fetchHeaderData
                headerData = await fetchHeaderData(reportId);
            } else {
                console.log(`[loadReportDetails] 查阅/提醒模式，使用fetchReportsReminder`);
                // 查阅/提醒模式：使用fetchReportsReminder
                headerData = await fetchReportsReminder(IdCard, type, reportId);
            }

            headerData = headerData[0].reports_TEST_REC[0]
            // 2. 尝试加载常规检验数据
            let detailsData = await fetchDetailsData(businessNo, reportId, currentMode);
            detailsData = detailsData.report_testr_res_indicate
            detailsDataReports = detailsData;
            if (detailsData && detailsData.length > 0) {
                renderReportDetails(headerData, detailsData, null, null);
                microbiaMark = "0";
                return;
            }

            // 3. 常规检验无数据，尝试加载微生物报告
            const microbialData = await fetchCombinedMicrobialData(businessNo, reportId);
            microbiaMark = "1";//微生物标记
            if (microbialData && (microbialData.bacteria || microbialData.drugs)) {
                renderReportDetails(headerData, null, microbialData, null);
                return;
            }

            // 4. 显示无数据提示
            renderNoData(headerData);
        } else {
            // 检查报告加载逻辑
            let examData;
            if (currentMode === "recognize") {
                // 互认模式：使用原fetchHeaderData
                examData = await fetchExamData(reportId);
            } else {
                // 查阅/提醒模式：使用fetchReportsReminder
                examData = await fetchReportsReminder(IdCard, type, reportId);
            }
            detailsDataReports = examData
            renderExamReportDetails(examData[0].report_CHECK_REC_oracle);
        }
    } catch (error) {
        console.error('加载详情失败:', error);
        showErrorMessage('reportDetails', `
            加载失败: ${error.message}
            <button onclick="loadReportDetails('${businessNo}','${reportId}','${type}')">点击重试</button>
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

        await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
    } catch (error) {
        await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
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
    // 返回响应对象
    return response;
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
// 单条互认操作
async function recognizeReport(event, button) {
    event.stopPropagation();
    try {
        let citemId = null;
        let citemName = null;
        let execDeptId = null;
        let specimenSite = null;
        let collectionName = null;
        const reportId = button.dataset.reportid;
        highlightAndLoadReport(reportId)
        targetreportId = reportId;

        // 从全局变量获取输入数据（JSON）
        const inputData = window.reportContext?.inputData?.input?.req_info || [];
        const hospitalReports = currentReports.filter(r => r.id === reportId);
        const mutualCode = hospitalReports[0]["dataCode"];
        const treatmentCode = inputData?.[0]?.["citem_id"];
        const treatmentName = inputData?.[0]?.["citem_name"];
        const patVisitType = inputData?.[0]?.["fee_source"];
        const operatorId = inputData?.[0]?.["apply_dept_id"];
        const operatorName = inputData?.[0]?.["placer_name"] ?? "系统管理员";

        // 获取配置
        const validationConfig = await getValidationConfig();
        const enableMappingValidation = validationConfig.enableMappingValidation;
        const enableDataWriteBack = validationConfig.enableDataWriteBack;

        console.log(`当前配置 - 对码校验: ${enableMappingValidation}, 数据回写: ${enableDataWriteBack}`);

        // 根据配置决定是否执行对码校验
        if (enableMappingValidation === 1) {
            try {
                const result1 = await QueryGLHRMapping(currentReportType, mutualCode);
                citemId = result1.data["mutualTreatmentId"];
                citemName = result1.data["mutualTreatmentName"];
                execDeptId = result1.data["execDeptId"];
                specimenSite = result1.data["specimenSite"];
                collectionName = result1.data["collectionName"];

                if (citemId === null) {
                    showToast('该项目未对码，请联系管理员,项目代码: ' + mutualCode, 'warning');
                    return;
                }
            } catch (quoteError) {
                showToast('该项目未对码，请联系管理员, ' + quoteError.message, 'warning');
                return;
            }
        } else {
            // 不校验对码，使用原始数据
            citemId = treatmentCode;
            citemName = treatmentName;
            console.log('对码校验已禁用，使用原始诊疗项目信息');
        }

        const RecognitionData = prepareRecognitionData(hospitalReports, currentReportType, "", citemId);
        const response = await PLupdateRemoRecognitionStatus(RecognitionData);

        // 处理成功结果
        const result = await response.json();
        if (result.success) {
            showToast(`成功互认 ${hospitalReports.length}份报告`);
        } else {
            showToast(`上传互认记录失败 :${result.message}`);
        }

        // 2. 互认成功后，执行引用记录上传
        //try {
        //    const quoteResponse = await uploadQuoteLog(RecognitionData);
        //    const quoteResult = await quoteResponse.json();
        //    if (!quoteResult.success) {
        //        if (result.success) { 
        //            showToast(`互认成功，但引用记录上传失败: ${quoteResult.message}`);
        //        }
        //        await loadUserReportList(PatientID, currentReportType);
        //        throw new Error('引用记录上传失败');
        //    }
        //    showToast(`成功互认 ${hospitalReports.length}份报告并上传引用记录`);
        //} catch (quoteError) {
        //    console.error('引用记录上传失败:', quoteError);
        //    //showToast('互认成功，但引用记录上传失败: ' + quoteError.message, 'warning');
        //    await loadUserReportList(PatientID, currentReportType);
        //    return;
        //}

        // 检查是否启用数据回写
        if (enableDataWriteBack === 0) {
            console.log('数据回写已禁用，跳过后续流程');
            //showToast('互认成功（数据回写已跳过）');
            await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
            return { success: true, message: '操作成功（数据回写已跳过）' };
        }

        // 以下是数据回写流程（enableDataWriteBack === 1 时执行）
        const toNumber = (value) => {
            if (value === null || value === undefined || value === '') return null;
            const num = Number(value);
            return isNaN(num) ? null : num;
        };

        // 创建新的输入数据，替换citem_id和citem_name
        const newInputData = {
            ...window.reportContext?.inputData,
            input: {
                ...window.reportContext?.inputData?.input,
                req_info: window.reportContext?.inputData?.input?.req_info?.map((item, index) => {
                    if (index === 0) {
                        return {
                            ...item,
                            citem_id: citemId,
                            citem_name: citemName,
                            citem_type: currentReportType === "lab" ? "C" : "D",
                            lspcm_name: currentReportType === "lab" ? specimenSite : "",
                            exedept_id: toNumber(execDeptId),
                            citem_name_cj: collectionName
                        };
                    }
                    return item;
                }) || []
            }
        };

        const quoteResponse = await callNewStandardService("S3112", newInputData);
        const exeStatus = quoteResponse.data.value.input.ack_info.exe_status;
        const reportTypeText = currentReportType === "lab" ? "检验" : "检查";

        if (exeStatus === 'A') {
            showToast(`发送${reportTypeText}医嘱成功: ${quoteResponse.message}`);
        } else if (exeStatus === 'E') {
            showToast(`发送${reportTypeText}医嘱失败: ${quoteResponse.message}`);
            throw new Error(`发送${reportTypeText}医嘱失败:${quoteResponse.data.value.input.ack_info.err_msg}`);
        }

        // 检验流程
        if (currentReportType === "lab") {
            // 生成条码
            const options = {
                patVisitType: feeSource,
                operatorId: doctorId,
                operatorName: operatorName
            };
            const callNewStandardServiceS4024Response = await callNewStandardServiceS4024('S4024', quoteResponse.data.value, options);
            const S4024ResponseStatus = callNewStandardServiceS4024Response.data.input.ack_info.exe_status;
            if (S4024ResponseStatus === 'A') {
                showToast(`检验条码生成成功`);
            } else if (S4024ResponseStatus === 'E') {
                showToast(`检验条码生成失败`);
                throw new Error(`检验条码生成失败:${callNewStandardServiceS4024Response.data.input.ack_info.err_msg}`);
            }

            // 采集流程
            const operatorTypes = ["2", "5", "10"];
            const s4009Responses = [];
            let previousResponseData = callNewStandardServiceS4024Response.data;

            for (let i = 0; i < operatorTypes.length; i++) {
                const currentOperatorType = operatorTypes[i];
                const optionsS4009 = {
                    operatorType: currentOperatorType,
                    operatorName: operatorName,
                    applyId: quoteResponse.data.value.input.apply_info[0].apply_id,
                    organizationName: "测试科室",
                    equipmentId: "1741"
                };

                try {
                    const response = await callNewStandardServiceS4009('S4009', previousResponseData, optionsS4009);
                    if (response.data.input.ack_info.exe_status === 'A') {
                        console.log(`第${i + 1}次采集操作成功 - 操作类型${currentOperatorType}`);
                        s4009Responses.push(response);
                    } else {
                        throw new Error(`第${i + 1}次采集失败: ${response.data.input.ack_info.err_msg}`);
                    }
                } catch (error) {
                    console.error(`第${i + 1}次采集过程失败:`, error);
                    throw error;
                }
            }

            // 检验数据回写
            const optionsS4010 = {
                applyId: quoteResponse.data.value.input.apply_info[0].apply_id,
                checker: operatorName,
                reporter: operatorName,
                equipmentId: "1741",
                reportId: reportId,
                doctorId: doctorId,
                patientID: PatientID,
            };

            try {
                const callNewStandardServiceS4010Response = await callNewStandardServiceS4010('S4010', detailsDataReports, callNewStandardServiceS4024Response.data, optionsS4010);
                if (callNewStandardServiceS4010Response.data.input.ack_info.exe_status === 'A') {
                    showToast(`检验数据回写成功`);
                } else {
                    showToast(`检验数据回写失败`);
                    // 回写失败取消条码
                    const optionsS4009 = {
                        operatorType: 4,
                        operatorName: operatorName,
                        applyId: quoteResponse.data.value.input.apply_info[0].apply_id,
                        organizationName: "测试科室",
                        equipmentId: "1741"
                    };
                    const response = await callNewStandardServiceS4009('S4009', previousResponseData, optionsS4009);
                    await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
                    throw new Error(`检验条码生成失败:${callNewStandardServiceS4010Response.data.input.ack_info.err_msg}`);
                }
            } catch (error) {
                console.error(`检验数据回写失败`);
                throw error;
            }
        } else {
            // 检查流程
            const optionsS5006 = {
                operatorName: operatorName,
                applyStatus: "3",
                exeProcess: "1",
            };

            try {
                const callNewStandardServiceS5006Response = await callNewStandardServiceS5006("S5006", quoteResponse.data.value, optionsS5006);
                if (callNewStandardServiceS5006Response.input.ack_info.exe_status === 'A') {
                    showToast(`检查申请状态回写成功`);
                } else {
                    showToast(`检查申请状态回写失败`);
                    await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
                    throw new Error(`检查申请状态回写失败:${callNewStandardServiceS5006Response.input.ack_info.err_msg}`);
                }
            } catch (error) {
                console.error(`检查申请状态回写失败`);
                throw error;
            }

            const requestData = {
                applyId: quoteResponse.data.value.input.apply_info[0].apply_id,
                applyStatus: "1",
                reporterName: operatorName,
                checkerName: operatorName,
                reportId: reportId,
                doctorId: doctorId,
                patientId: PatientID,
                reportResults: [
                    {
                        category: "检查所见",
                        content: detailsDataReports[0].report_CHECK_REC_oracle[0].checkResObj
                    },
                    {
                        category: "诊断意见",
                        content: detailsDataReports[0].report_CHECK_REC_oracle[0].checkResSub
                    }
                ],
                reportFiles: []
            };

            const callNewStandardServiceS5008Response = await callNewStandardServiceS5008('S5008', requestData);
            if (callNewStandardServiceS5008Response.input.ack_info.exe_status === 'A') {
                showToast(`检查数据回写成功`);
            } else {
                showToast(`检查数据回写失败`);
                await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
                throw new Error(`检查数据回写失败:${callNewStandardServiceS5008Response.input.ack_info.err_msg}`);
            }
        }

        await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
        return { success: true, message: '操作成功' };
    } catch (error) {
        console.error('互认操作失败:', error);
        showToast('互认操作失败: ' + error.message, 'error');
        return { success: false, error: error.message };
    }
}

// 获取校验配置的函数
async function getValidationConfig() {
    try {
        const response = await fetch('/api/Config/validation-config');
        if (!response.ok) {
            throw new Error('获取配置失败');
        }
        return await response.json();
    } catch (error) {
        console.error('获取校验配置失败，使用默认配置:', error);
        // 默认启用对码校验，禁用数据回写
        return {
            enableMappingValidation: 1,
            enableDataWriteBack: 0
        };
    }
}
// 引用记录上传函数 
async function uploadQuoteLog(reminderData) {
    try {
        const response = await fetch('/api/Person/upload-quote-log', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(reminderData)
        });

        // 检查响应状态
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        // 检查响应内容类型
        const contentType = response.headers.get('content-type');
        if (!contentType || !contentType.includes('application/json')) {
            // 如果不是 JSON，获取文本内容
            const text = await response.text();
            console.error('Response is not JSON:', text);
            throw new Error('服务器返回的不是 JSON 格式');
        }

        // 尝试解析 JSON
        let result;
        try {
            result = await response.json();
        } catch (jsonError) {
            console.error('JSON parse error:', jsonError);
            throw new Error('响应 JSON 解析失败');
        }

        if (!result.success) {
            // 返回具体的错误信息
            const errorMsg = result.message || result.msg || '引用记录上传失败';
            throw new Error(errorMsg);
        }

        return result;
    } catch (error) {
        console.error('uploadQuoteLog error:', error);
        throw error;
    }
}
// 获取互认项目对应的诊疗项目ID
/**
 * 查询GLHR检查检验映射表（通过GET请求）
 * @param {string} mutualCode 互认编码
 * @param {string} treatmentCode 诊疗项目编码
 * @param {string} [part] 检查部位（可选）
 * @param {string} [method] 检查方法（可选）
 * @returns {Promise<object>} 返回API响应数据
 * @throws 当请求失败时抛出错误
 */
async function QueryGLHRMapping(type,mutualCode, treatmentCode, part, method) {
    // 1. 构建查询参数
    const params = new URLSearchParams();
    params.append('type', type);       // 必填参数：查询类型
    params.append('mutualCode', mutualCode);       // 必填参数：互认编码
    params.append('treatmentCode', treatmentCode); // 必填参数：诊疗项目编码
    if (part) params.append('part', part);         // 可选参数：检查部位
    if (method) params.append('method', method);    // 可选参数：检查方法

    // 2. 拼接完整URL（带查询参数）
    const url = `/api/Person/query-glhr-mapping?${params.toString()}`;

    try {
        // 3. 发送GET请求
        const response = await fetch(url, {
            method: 'GET',  // 注意：后端是[HttpGet]方法
            headers: {
                'Content-Type': 'application/json'
            }
        });

        // 4. 检查响应状态
        if (!response.ok) {
            throw new Error(`请求失败，状态码：${response.status}`);
        }

        // 5. 解析并返回JSON数据
        return await response.json();
    } catch (error) {
        // 6. 统一错误处理
        console.error('查询GLHR映射时出错:', error);
        throw new Error(`查询失败: ${error.message}`);
    }
}
//查询病人关键信息
async function QueryPatientInfo(GHID) {
    // 1. 构建查询参数
    const params = new URLSearchParams();
    params.append('GHID', GHID);  // 必填参数：挂号ID

    // 2. 拼接完整URL（带查询参数）
    const url = `/api/Person/query-patient-info?${params.toString()}`;

    try {
        // 3. 发送GET请求
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        // 4. 检查响应状态
        if (!response.ok) {
            throw new Error(`请求失败，状态码：${response.status}`);
        }

        // 5. 解析并返回JSON数据
        return await response.json();
    } catch (error) {
        // 6. 统一错误处理
        console.error('查询病人基本信息时出错:', error);
        throw new Error(`查询失败: ${error.message}`);
    }
}
/**
 * 调用新标准服务通用接口
 * @param {string} serviceMethod 服务方法名
 * @param {object} requestData 请求数据
 * @returns {Promise<object>} 返回API响应数据
 */
async function callNewStandardService(serviceMethod, requestData) {
    try {
        const response = await fetch(`/api/Person/call-new-standard/${serviceMethod}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            throw new Error(`请求失败，状态码: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error(`调用新标准服务[${serviceMethod}]失败:`, error);
        throw new Error(`服务调用失败: ${error.message}`);
    }
}
/**
 * 调用新标准服务 S3112二号接口 - 开单申请
 * @param {string} serviceMethod - 服务方法名
 * @param {object} requestData - 请求数据
 * @returns {Promise<object>} 响应结果
 */
async function callNewStandardServiceS3112V2(serviceMethod, requestData) {
    try {
        const url = `/api/Person/call-new-S3112-v2/${serviceMethod}`;

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            throw new Error(`请求失败，状态码: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error(`调用新标准服务[${serviceMethod}]失败:`, error);
        throw new Error(`服务调用失败: ${error.message}`);
    }
}
/**
 * 调用新标准服务 S4024
 * @param {string} serviceMethod - 服务方法名
 * @param {object} requestData - 请求数据
 * @param {object} options - 可选参数
 * @param {string} options.patVisitType - 病人来源，默认"1"
 * @param {string} options.operatorId - 操作员ID，默认"2355"
 * @param {string} options.operatorName - 操作员姓名，默认"系统管理员"
 * @param {string} options.outpno - 门诊号
 * @param {string} options.idno - 身份证号
 * @param {string} options.startDate - 开始日期
 * @param {string} options.endDate - 结束日期
 * @returns {Promise<object>} 响应结果
 */
async function callNewStandardServiceS4024(serviceMethod, requestData, options = {}) {
    try {
        // 构建查询参数
        const queryParams = new URLSearchParams();

        // 添加可选参数
        if (options.patVisitType) queryParams.append('patVisitType', options.patVisitType);
        if (options.operatorId) queryParams.append('operatorId', options.operatorId);
        if (options.operatorName) queryParams.append('operatorName', options.operatorName);
        if (options.outpno) queryParams.append('outpno', options.outpno);
        if (options.idno) queryParams.append('idno', options.idno);
        if (options.startDate) queryParams.append('startDate', options.startDate);
        if (options.endDate) queryParams.append('endDate', options.endDate);

        const url = `/api/Person/call-new-S4024/${serviceMethod}${queryParams.toString() ? '?' + queryParams.toString() : ''}`;

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            throw new Error(`请求失败，状态码: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error(`调用新标准服务[${serviceMethod}]失败:`, error);
        throw new Error(`服务调用失败: ${error.message}`);
    }
}
/**
 * 调用新标准服务 S4008 - 作废申请单
 * @param {string} serviceMethod - 服务方法名
 * @param {object} requestData - 请求数据
 * @returns {Promise<object>} 响应结果
 */
async function callNewStandardServiceS4008(serviceMethod, requestData) {
    try {
        const url = `/api/Person/call-new-S4008/${serviceMethod}`;

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            throw new Error(`请求失败，状态码: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error(`调用新标准服务[${serviceMethod}]失败:`, error);
        throw new Error(`服务调用失败: ${error.message}`);
    }
}
/**
 * 调用新标准服务 S4009
 * @param {string} serviceMethod - 服务方法名
 * @param {object} requestData - 请求数据
 * @param {object} options - 必填和可选参数
 * @param {string} options.operatorType - 操作类型（必填）
 * @param {string} options.operatorName - 操作员姓名（必填）
 * @param {string} options.applyId - 医嘱相关ID,来自S3112出参
 * @param {string} options.organizationName - 操作科室名称
 * @param {string} options.equipmentId - 仪器ID
 * @returns {Promise<object>} 响应结果
 */
async function callNewStandardServiceS4009(serviceMethod, requestData, options = {}) {
    try {
        // 参数验证
        if (!options.operatorType) {
            throw new Error('操作类型(operatorType)不能为空');
        }
        if (!options.operatorName) {
            throw new Error('操作员姓名(operatorName)不能为空');
        }

        // 构建查询参数
        const queryParams = new URLSearchParams();
        queryParams.append('operatorType', options.operatorType);
        queryParams.append('operatorName', options.operatorName);
        queryParams.append('applyId', options.applyId);

        if (options.organizationName) queryParams.append('organizationName', options.organizationName);
        if (options.equipmentId) queryParams.append('equipmentId', options.equipmentId);

        const url = `/api/Person/call-new-S4009/${serviceMethod}?${queryParams.toString()}`;

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData) 
        });

        if (!response.ok) {
            throw new Error(`请求失败，状态码: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error(`调用新标准服务[${serviceMethod}]失败:`, error);
        throw new Error(`服务调用失败: ${error.message}`);
    }
}
/**
 * 调用新标准服务 S4009二号接口
 * @param {string} serviceMethod - 服务方法名
 * @param {object} requestData - 请求数据
 * @returns {Promise<object>} 响应结果
 */
async function callNewStandardServiceS4009V2(serviceMethod, requestData) {
    try {
        const url = `/api/Person/call-new-S4009-v2/${serviceMethod}`;

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            throw new Error(`请求失败，状态码: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error(`调用新标准服务[${serviceMethod}]失败:`, error);
        throw new Error(`服务调用失败: ${error.message}`);
    }
}
/**
 * 调用新标准服务 S4010 - 检验报告上传
 * @param {string} serviceMethod - 服务方法名
 * @param {array} reportData - 检验报告数据数组
 * @param {object} additionalInfo - 附加信息（S4024的出参格式）
 * @param {object} options - 参数
 * @param {string} options.applyId - 申请单ID（必填）
 * @param {string} options.checker - 审核人
 * @param {string} options.reporter - 报告人
 * @param {string} options.equipmentId - 仪器ID
 * @param {string} options.doctorId - 医生ID
 * @param {string} options.patientID - 病人ID
 * @returns {Promise<object>} 响应结果
 */
async function callNewStandardServiceS4010(serviceMethod, reportData, additionalInfo, options = {}) {
    try {
        // 参数验证
        if (!options.applyId) {
            throw new Error('申请单ID(applyId)不能为空');
        }

        // 构建请求体
        const requestBody = {
            reportData: reportData,
            additionalInfo: additionalInfo
        };

        // 构建查询参数
        const queryParams = new URLSearchParams();
        queryParams.append('applyId', options.applyId);

        if (options.checker) queryParams.append('checker', options.checker);
        if (options.reporter) queryParams.append('reporter', options.reporter);
        if (options.equipmentId) queryParams.append('equipmentId', options.equipmentId);
        if (options.reportId) queryParams.append('reportId', options.reportId);
        if (options.reportId) queryParams.append('doctorId', options.doctorId);
        if (options.reportId) queryParams.append('patientID', options.patientID);

        const url = `/api/Person/call-new-S4010/${serviceMethod}?${queryParams.toString()}`;

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) {
            throw new Error(`请求失败，状态码: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error(`调用新标准服务[${serviceMethod}]失败:`, error);
        throw new Error(`服务调用失败: ${error.message}`);
    }
}
/**
 * 调用新标准服务 S4011 - 样本信息查询/操作
 * @param {string} serviceMethod - 服务方法名
 * @param {object} requestData - 请求数据
 * @returns {Promise<object>} 响应结果
 */
async function callNewStandardServiceS4011(serviceMethod, requestData) {
    try {
        const url = `/api/Person/call-new-S4011/${serviceMethod}`;

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            throw new Error(`请求失败，状态码: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error(`调用新标准服务[${serviceMethod}]失败:`, error);
        throw new Error(`服务调用失败: ${error.message}`);
    }
}
/**
 * 调用新标准服务 S5006 - 检查报到状态更新
 * @param {string} serviceMethod - 服务方法名
 * @param {object} requestData - 请求数据
 * @param {object} options - 参数
 * @param {string} options.operatorName - 操作员姓名（必填）
 * @param {string} options.applyStatus - 执行状态	执行状态|3-正在执行;1-执行完成;0-未执行
 * @param {string} options.exeProcess - 执行过程 执行过程|-1-驳回；0或1-已登记；2-已报到；3-已检查；4-已报告；5-已审核；6-已完成
 * @param {string} options.exeRoom - 执行间
 * @returns {Promise<object>} 响应结果
 */
async function callNewStandardServiceS5006(serviceMethod, requestData, options = {}) {
    try {
        // 参数验证
        if (!options.operatorName) {
            throw new Error('操作员姓名(operatorName)不能为空');
        }

        // 构建查询参数
        const queryParams = new URLSearchParams();
        queryParams.append('operatorName', options.operatorName);

        if (options.applyStatus) queryParams.append('applyStatus', options.applyStatus);
        if (options.exeProcess) queryParams.append('exeProcess', options.exeProcess);
        if (options.exeRoom) queryParams.append('exeRoom', options.exeRoom);

        const url = `/api/Person/call-new-S5006/${serviceMethod}?${queryParams.toString()}`;

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            throw new Error(`请求失败，状态码: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error(`调用新标准服务[${serviceMethod}]失败:`, error);
        throw new Error(`服务调用失败: ${error.message}`);
    }
}
/**
 * 调用新标准服务 S5008 - 检查报告上传
 * @param {string} serviceMethod - 服务方法名
 * @param {object} requestData - 请求数据
 * @returns {Promise<object>} 响应结果
 */
async function callNewStandardServiceS5008(serviceMethod, requestData) {
    try {
        const url = `/api/Person/call-new-S5008/${serviceMethod}`;

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            throw new Error(`请求失败，状态码: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error(`调用新标准服务[${serviceMethod}]失败:`, error);
        throw new Error(`服务调用失败: ${error.message}`);
    }
}
/**
 * 调用新标准服务 S5009 - 取消检查报告
 * @param {string} serviceMethod - 服务方法名
 * @param {object} requestData - 请求数据
 * @returns {Promise<object>} 响应结果
 */
async function callNewStandardServiceS5009(serviceMethod, requestData) {
    try {
        const url = `/api/Person/call-new-S5009/${serviceMethod}`;

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            throw new Error(`请求失败，状态码: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error(`调用新标准服务[${serviceMethod}]失败:`, error);
        throw new Error(`服务调用失败: ${error.message}`);
    }
}
// 撤销互认操作
async function revokeRecognition(event, button) {
    try {
        // 从全局变量获取输入数据（JSON）
        const inputData = window.reportContext?.inputData?.input?.req_info || [];
        const operatorName = inputData?.[0]?.["placer_name"] ?? "系统管理员";

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
        // 获取配置
        const validationConfig = await getValidationConfig();
        const enableMappingValidation = validationConfig.enableMappingValidation;
        const enableDataWriteBack = validationConfig.enableDataWriteBack;

        const getApplyId = await fetchRecognitionApplyId(PatientID, reportId, doctorId);
        const GetSpcmbcNo = await fetchRecognitionSpcmbcNo(getApplyId.applyId);
        const requestData = {
            applyId: getApplyId.applyId,
            operatorName: operatorName
        };
        // 检查是否启用数据回写
        if (enableDataWriteBack === 0) {
            console.log('数据回写已禁用，跳过后续流程');
            await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
            return { success: true, message: '操作成功（数据回写已跳过）' };
        }
        if (currentReportType === "exam") {
            const callNewStandardServiceS5009Response = await callNewStandardServiceS5009("S5009", requestData);
            if (callNewStandardServiceS5009Response.input.ack_info.exe_status === 'A') {
                showToast(`取消检查报告信息成功`);
            } else {

                showToast(`取消检查报告信息失败`);
                throw new Error(`取消检查报告信息失败:${callNewStandardServiceS5009Response.input.ack_info.err_msg}`);
                //return;
            }
        }
        else
        {
            const optionsS4011 = {
                applyId: getApplyId.applyId,
                specimenBarcodeNo: GetSpcmbcNo.SpcmbcNo,
                operatorName: operatorName
            };
            try {

                const callNewStandardServiceS4011response = await callNewStandardServiceS4011('S4011', optionsS4011);
                if (callNewStandardServiceS4011response.input.ack_info.exe_status === 'A') {
                    console.log(`撤销报告成功`);
                } else {
                    console.log(`撤销报告失败`);
                    //throw new Error(`撤销报告失败: ${response.data.input.ack_info.err_msg}`);
                }
            } catch (error) {
                console.error(`撤销报告失败:`);
                throw error;
            }



            try {
                const optionsS4009 = {
                    applyId: getApplyId.applyId,
                    operatorType: "3",
                    specimenBarcodeNo: GetSpcmbcNo.SpcmbcNo,
                    operatorName: operatorName,
                    organizationName: "",
                    equipmentId: ""
                };

                const callNewStandardServiceS4009V2response = await callNewStandardServiceS4009V2('S4009', optionsS4009);

                if (callNewStandardServiceS4009V2response.input.ack_info.exe_status === 'A') {
                    console.log(`撤销条码成功`);
                    //s4009Responses.push(response);
                    // 更新下一次调用的数据（如果需要使用前一次的返回数据）
                    // previousResponseData = response.data || previousResponseData;
                } else {
                    throw new Error(`撤销条码失败: ${callNewStandardServiceS4009V2response.input.ack_info.err_msg}`);
                    await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
                    return;
                }
            } catch (error) {
                console.error(`撤销条码失败:`);
                throw error;
            }
            try {
                const optionsS4008 = {
                    patientId: PatientID,
                    patientVisitType: feeSource, // 1-门诊
                    registrationNo: RegistrationNo,
                    applyId: getApplyId.applyId,
                    placerName: DoctorName
                };
                const callNewStandardServiceS4008response = await callNewStandardServiceS4008('S4008', optionsS4008);
                if (callNewStandardServiceS4008response.input.ack_info.exe_status === 'A') {
                    console.log(`作废检验医嘱成功`);
                } else {
                    console.log(`作废检验医嘱失败`);
                    //throw new Error(`作废检验医嘱失败: ${response.data.input.ack_info.err_msg}`);
                }
            } catch (error) {
                console.error(`作废检验医嘱失败:`);
                throw error;
            }
        }
        await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
    } catch (error) {
        console.error('撤销互认操作失败:', error);
        showToast('撤销互认操作失败: ' + error.message, 'error');
        return { success: false, error: error.message }; // 明确返回错误信息
    }
}
async function fetchRecognitionApplyId(userId, reportId, doctorId) {
    try {
        const response = await fetch(`/api/Person/apply-id/${userId}/${reportId}/${doctorId}`);
        if (response.ok) {
            const data = await response.json();
            const dataArray = Object.entries(data);
            console.log("Data 对象转数组:", dataArray);
            return {
                applyId: data.applyId
            };
        }
        return {
            applyId: null
        };
    } catch (e) {
        console.error(`获取报告 ${reportId} 状态失败:`, e);
        return false;
    }
}
async function fetchRecognitionSpcmbcNo(ApplyId) {
    try {
        const response = await fetch(`/api/Person/Spcmbc-No/${ApplyId}`);
        if (response.ok) {
            const data = await response.json();
            const dataArray = Object.entries(data);
            console.log("Data 对象转数组:", dataArray);
            return {
                SpcmbcNo: data.spcmbcNo
            };
        }
        return {
            SpcmbcNo: null
        };
    } catch (e) {
        console.error(`获取报告 ${ApplyId} 条码失败:`, e);
        return false;
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
        const response = await PLupdateRecognitionStatus(RecognitionData);

        // 处理响应
        const result = await response.json();

        if (response.ok && result.success) {
            showToast(`成功不互认 ${hospitalReports.length}份报告`);
        } else {
            // 显示具体的错误信息
            const errorMessage = result.message || result.msg || '不互认操作失败';
            showToast(`不互认操作失败: ${errorMessage}`, 'error');
            // 不抛出错误，防止进入catch块
        }

        await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
    } catch (error) {
        console.error('不互认操作失败:', error);
        showToast('不互认操作失败: ' + error.message, 'error');
        return { success: false, error: error.message };
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
        await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
    } catch (error) {
        console.error('撤销互认操作失败:', error);
        showToast('撤销互认操作失败: ' + error.message, 'error');
        return { success: false, error: error.message }; // 明确返回错误信息
    }

}
// 引用报告函数  
// 引用报告函数  
async function quoteReport(event, button) {
    try {
        event.stopPropagation(); // 阻止事件冒泡

        const reportId = button.dataset.reportid;
        const hospitalName = button.dataset.hospital;
        const projectName = button.dataset.project;

        // 确认弹窗
        if (!confirm(`确定要引用"${hospitalName}"的"${projectName}"报告吗？`)) {
            return;
        }

        // 高亮并加载报告详情
        highlightAndLoadReport(reportId);

        // 从全局变量获取输入数据（JSON）
        const inputData = window.reportContext?.inputData?.input?.req_info || [];
        const hospitalReports = currentReports.filter(r => r.id === reportId);
        const treatmentCode = inputData?.[0]?.["citem_id"];
        // 构建引用数据
        const RecognitionData = prepareRecognitionData(hospitalReports, currentReportType, "", treatmentCode);
        // 构建引用数据
        //const quoteData = {
        //    reportId: reportId,
        //    patientId: PatientID, // 使用全局变量
        //    patientName: document.getElementById('patient-name').textContent,
        //    patientIdCard: document.getElementById('patient-id').textContent,
        //    reportType: currentReportType, // 'lab' 或 'exam'
        //    hospitalName: hospitalName,
        //    projectName: projectName,
        //    quoteTime: new Date().toISOString(),
        //    operatorId: currentUserId, // 当前操作用户ID
        //    operatorName: currentUserName // 当前操作用户名
        //};

        // 调用引用记录上传接口 - uploadQuoteLog 已经返回解析后的对象
        const quoteResult = await uploadQuoteLog(RecognitionData);


        // 检查结果
        if (quoteResult && quoteResult.success) {
            showToast(`引用记录上传成功!`);

            // 更新按钮状态
            button.classList.remove('btn-quote');
            button.classList.add('btn-quoted');
            button.textContent = '已引用';
            button.disabled = true;
            button.onclick = null;

            // 刷新报告列表
            await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);

            return { success: true, data: quoteResult.data };
        } else {
            // 如果 quoteResult 是 undefined 或没有 success 属性
            const errorMsg = quoteResult?.message || quoteResult?.msg || '引用记录上传失败';
            throw new Error(errorMsg);
        }
    } catch (error) {
        console.error('报告引用失败:', error.message);
        showToast('报告引用失败: ' + error.message, 'error');
        return { success: false, error: error.message };
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
function prepareRecognitionData(reports, type, reasonData = null, citemId = null, microbiaMark = "0") {
    if (!reports || reports.length === 0) return [];
    const urlParams = new URLSearchParams(window.location.search);
    // 从全局变量获取输入数据（JSON）
    const inputData = window.reportContext?.inputData?.input?.req_info || [];
    //const mutualCode = reports[0]["dataCode"];
    //const treatmentCode = inputData[0]["citem_id"]; 
    return reports.map(report => {

        // 根据dataCode匹配orderId
        const matchedOrder = inputData.find(item =>
            item.dataCode.toString() === report.dataCode?.toString()
        );
        const orderId = matchedOrder?.orderId ? matchedOrder.orderId.toString() : "";
        
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
            idCardTypeCode: report.idCardTypeCode,
            doctorId: doctorId,
            orderId: orderId,
            citemId: citemId !== null ? citemId.toString() : null,  // 直接转为字符串，不进行数字转换
            MicrobiaMark: microbiaMark
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
    let targetreportId = urlParams.get('reportId');
    // 先克隆再清空
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
                <!--<div class="recognition-actions">
                    <button class="recognition-btn btn-recognize" >全部互认</button>
                    <button class="recognition-btn btn-reject">全部不互认</button>
                </div>>-->
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
                //loadUserReportList(PatientID, currentReportType)
            }

            // 非按钮区域点击加载详情
            if (!e.target.closest('.recognition-btn')) {
                document.querySelectorAll('.report-item').forEach(i => i.classList.remove('active'));
                reportItem.classList.add('active');
                if (report) {
                    loadReportDetails(report[0].businessNo, report[0].id, currentReportType);
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
            const responseResult = await response.json();
            console.log('接口返回结果:', responseResult);
            if (!response.ok) {
                showToast('上传调阅记录失败:' + responseResult.message);
                throw new Error('上传调阅记录失败');
            }
            if (!responseResult.success) {
                showToast('上传调阅记录失败:' + responseResult.message);
                return;
            }

            if (responseResult.success) {
                showToast(`调阅记录上传成功,数量:${reports.length}`);
                await loadUserReportList(PatientID, currentReportType, IdCard, dataCode); // 
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
            <h2>${data.orgName || '未知医疗机构'}</h2>
            <h3>${data.testProjCategoryName || '检验报告'}</h3>
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
            <td>${item.test_proj_name_exp || '-'}</td>
            <td class="${item.anomaly_code !== '1' ? 'abnormal' : 'normal'}">
                ${item.test_index_result || '-'}
            </td>
            <td>${item.test_index_uint || '-'}</td>
            <td>${item.normal_ref_limit || '-'}</td>
            <td class="${item.anomaly_code !== '1' ? 'abnormal' : 'normal'}">
                ${item.anomaly_name || (item.anomaly_code === '1' ? '正常' : '异常')}
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
                ${item.testResult || '-'}
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
            <td>${item.bacteriostaticConcentrate || '-'}</td>
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
                <th width="15%">抑菌浓度</th>
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
            loadReportDetails(report.businessNo, report.id, currentReportType);
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
    //const url = new URL('/api/Products/detail', window.location.origin);
    const url = new URL('/api/Person/report-headers-oracle', window.location.origin);
    url.searchParams.append('reportId', reportId);
    const data = await fetchData(url);
    if (!data) throw new Error('表头数据加载失败');
    return data;
}

async function fetchDetailsData(businessNo, reportId, currentMode) {
    if (!reportId) return null;
    try {
        //const url = new URL(`/api/Products/report-details/${TestReportNo}/${reportId}`, window.location.origin);
        const url = new URL(`/api/Person/report-details-oracle/${businessNo}/${reportId}/${currentMode}`, window.location.origin);
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
    // 更新URL参数，移除reportId
    updateUrlParams({
        type: type,
        reportId: null  // 这将移除reportId参数
    });
    await loadUserReportList(PatientID, type, IdCard, dataCode);
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
            <h3>检查诊断</h3>
            <div class="exam-findings">${data[0].checkResSub || '无'}</div>
            <h3>检查结论</h3>
            <div class="exam-conclusion">${data[0].checkRes || '无'}</div>
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
    //const url = new URL('/api/Products/detail', window.location.origin);
    const url = new URL('/api/Person/report-headers-oracle', window.location.origin);
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

async function fetchCombinedMicrobialData(businessNo, reportId) {
    try {

        //const url = new URL(`/api/Products/microbial-combined-report/${TestReportNo}/${reportId}`, window.location.origin);
        const url = new URL(`/api/Person/microbial-report-oracle/${businessNo}/${reportId}`, window.location.origin);
        const response = await fetch(url);

        if (!response.ok) throw new Error(await response.text());

        const data = await response.json();

        // 处理返回的数据
        if (data.report_TMICROBE_BACTERIA_RES && data.report_TMICROBE_BACTERIA_RES.length > 0) {
            // 有微生物培养结果
            if (data.report_TMICROBE_SUSCEPT_RES) {
                // 有药敏结果
                return {
                    header: data.reports_TEST_REC,
                    bacteria: data.report_TMICROBE_BACTERIA_RES,
                    drugs: data.report_TMICROBE_SUSCEPT_RES
                };
            } else {
                // 只有微生物培养结果
                return {
                    header: data.reports_TEST_REC,
                    bacteria: data.report_TMICROBE_BACTERIA_RES,
                    drugs: null
                };
            }
        } else {
            // 只有基本信息
            return {
                header: data.reports_TEST_REC,
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
                    <span class="info-value">${data.patientName || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">性别：</span>
                    <span class="info-value">${data.genderName || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">年龄：</span>
                    <span class="info-value">${calculateAge(data?.birthDate) || '-'}</span>
                </div>
            </div>
            
            <div class="info-row">
                <div class="info-item">
                    <span class="info-label">病历号：</span>
                    <span class="info-value">${data.patNo || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">科室：</span>
                    <span class="info-value">${data.testApplyDepartNameExp || '-'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">床号：</span>
                    <span class="info-value">${data.bedNo || '-'}</span>
                </div>
            </div>
            
            <div class="info-row">
                <div class="info-item" style="min-width:100%">
                    <span class="info-label">临床诊断：</span>
                    <span class="info-value">${data.reportClinialDiag || '未填写'}</span>
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
                <span><strong>检验医生：</strong> ${data.testDoctName || '-'}</span>
                <span><strong>审核医生：</strong> ${data.auditDoctName || '-'}</span>
            </div>
            <div class="footer-row">
                <span><strong>报告时间：</strong> ${formatDateTime(data.testReportDate) || '-'}</span>
                <span><strong>采样时间：</strong> ${formatDateTime(data.sampleTime) || '-'}</span>
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
        const viewStatus = report.view_STATUS;
        const quoteStatus = report.quote_STATUS || '0'; // 引用状态，默认0表示未引用
        const searchType = report.searchType;
        let buttons = '';

        if (currentMode === 'view') {
            if (searchType === '1') {
                // 查看模式下
                buttons = `
                <div class="report-actions">
                    <button class="recognition-btn btn-viewed"
                            data-reportid="${report.id || report.reportId}"
                            onclick="revokeRecognition(event, this)">
                        已查阅
                    </button>
                </div>
                `;
            }
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

                    ${/*
                    // 引用按钮（已注释，暂不启用）
                    quoteStatus === '1' ?
                        '<button class="recognition-btn btn-quoted" disabled>已引用</button>' :
                        `<button class="recognition-btn btn-quote" 
                                data-reportid="${report.id || report.reportId}"
                                data-hospital="${report.orgName}"
                                data-project="${type === 'lab' ? report.testProjCategoryName : report.checkProjNameExp}"
                                onclick="quoteReport(event, this)">
                            引用
                         </button>`
                    */ ''}
                </div>
            `;
            }
            else if (status === '0') {
                // 不互认状态 - 显示撤销不互认按钮（撤销后回到未处理状态）
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
                // 未处理状态 - 显示互认/不互认按钮
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
    await loadUserReportList(PatientID, currentReportType, IdCard, dataCode);
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
//const REASON_OPTIONS = [
//    { code: "01", name: "因病情变化，检查检验结果与患者临床表现、疾病诊断不符，难以满足临床诊疗需求的" },
//    { code: "02", name: "检查检验结果在疾病发展演变过程中变化较快的" },
//    { code: "03", name: "检查检验项目对于疾病诊疗意义重大的（如手术、输血等重大医疗措施前）" },
//    { code: "04", name: "患者病情处于急诊、急救、重型、危重型等紧急状态下的" },
//    { code: "05", name: "涉及司法、伤残、病退、劳动仲裁等鉴定的" },
//    { code: "06", name: "国家规定需要相应资质的检测机构的检查检验项目" },
//    { code: "07", name: "诊治医师认为有必要进行再次检查的" },
//    { code: "08", name: "其他情形确需复查的" },
//    { code: "98", name: "医院未收到或患者未能提供外院检查影像资料无法进行诊断的（该原因不计入【不互认】)" },
//    { code: "99", name: "由于疾病诊断或病情发展监测需要进行连续性检查/检验（该原因不计入【不互认】)"}
//];

//// 显示原因选择弹窗
//// 修改后的 showReasonDialog 函数
//function showReasonDialog() {
//    return new Promise((resolve) => {
//        // 创建弹窗元素
//        const dialog = document.createElement('div');
//        dialog.className = 'reason-dialog';
//        dialog.innerHTML = `
//            <h3>请选择不互认原因</h3>
//            <div class="reason-options">
//                ${REASON_OPTIONS.map(option => `
//                    <div class="reason-option">
//                        <input type="radio" id="reason-${option.code}" name="reason" value="${option.code}">
//                        <label for="reason-${option.code}">${option.code} - ${option.name}</label>
//                    </div>
//                `).join('')}
//                <div class="disease-input">
//                    <input type="text" id="disease-name" placeholder="请输入疾病名称" required>
//                    <div class="error-message" style="color: #e74c3c; font-size: 12px; margin-top: 5px; display: none;">请填写疾病名称</div>
//                </div>
//            </div>
//            <div class="dialog-buttons">
//                <button class="cancel-btn">取消</button>
//                <button class="confirm-btn">确定</button>
//            </div>
//        `;

//        // 创建遮罩层
//        const overlay = document.createElement('div');
//        overlay.className = 'dialog-overlay';

//        // 添加到body
//        document.body.appendChild(overlay);
//        document.body.appendChild(dialog);

//        // 获取DOM元素
//        const diseaseInput = dialog.querySelector('.disease-input');
//        const diseaseNameInput = dialog.querySelector('#disease-name');
//        const errorMessage = dialog.querySelector('.error-message');
//        const confirmBtn = dialog.querySelector('.confirm-btn');

//        // 事件监听
//        const radioInputs = dialog.querySelectorAll('input[type="radio"]');
//        radioInputs.forEach(input => {
//            input.addEventListener('change', (e) => {
//                diseaseInput.style.display = e.target.value === '99' ? 'block' : 'none';
//                errorMessage.style.display = 'none'; // 隐藏错误提示
//            });
//        });

//        dialog.querySelector('.cancel-btn').addEventListener('click', () => {
//            document.body.removeChild(overlay);
//            document.body.removeChild(dialog);
//            resolve(null); // 取消操作
//        });

//        confirmBtn.addEventListener('click', () => {
//            const selected = dialog.querySelector('input[name="reason"]:checked');
//            if (!selected) {
//                showToast('请选择不互认原因', 'error');
//                return;
//            }

//            const reasonCode = selected.value;
//            const reasonName = REASON_OPTIONS.find(o => o.code === reasonCode).name;
//            const diagName = reasonCode === '99'
//                ? diseaseNameInput.value.trim()
//                : '';

//            // 新增验证：如果选择99但未填写疾病名称
//            if (reasonCode === '99' && !diagName) {
//                errorMessage.style.display = 'block';
//                diseaseNameInput.focus();
//                return;
//            }

//            document.body.removeChild(overlay);
//            document.body.removeChild(dialog);

//            resolve({ reasonCode, reasonName, diagName });
//        });

//        // 添加疾病名称输入的键盘事件监听
//        diseaseNameInput.addEventListener('keyup', (e) => {
//            if (e.key === 'Enter') {
//                confirmBtn.click();
//            }
//        });
//    });
//}
// ==================== 互认提醒功能 ====================
//功能特点

//智能匹配：通过dataCode匹配外部传入的JSON数据和报告数据

//直观提示：右上角显示匹配数量，点击可查看详情

//快速跳转：点击提醒项可直接跳转到对应报告

//自动类型切换：如果需要查看的报告类型与当前不同，会自动切换


//使用流程
//页面加载后解析URL中的JSON参数

//系统自动匹配dataCode并找到对应的报告

//右上角显示匹配的报告数量

//用户点击提醒图标查看匹配的报告列表

//点击列表项可直接跳转到对应报告详情页面

// 初始化提醒系统
function initReminderSystem() {
    const reminderIcon = document.getElementById('reminderIcon');
    const reminderPopup = document.getElementById('reminderPopup');
    const closeReminder = document.getElementById('closeReminder');
    const reminderList = document.getElementById('reminderList');

    // 点击提醒图标显示/隐藏弹窗
    reminderIcon.addEventListener('click', function (e) {
        e.stopPropagation();
        if (reminderPopup.style.display === 'block') {
            reminderPopup.style.display = 'none';
        } else {
            reminderPopup.style.display = 'block';
            renderReminderList();
        }
    });

    // 点击关闭按钮隐藏弹窗
    closeReminder.addEventListener('click', function () {
        reminderPopup.style.display = 'none';
    });

    // 点击页面其他区域关闭弹窗
    document.addEventListener('click', function (e) {
        if (!reminderPopup.contains(e.target) && e.target !== reminderIcon &&
            !reminderIcon.contains(e.target)) {
            reminderPopup.style.display = 'none';
        }
    });

    // 初始查找匹配的报告
    findMatchingReports();
}

// 查找匹配的报告
function findMatchingReports() {
    const inputData = window.reportContext?.inputData?.input?.req_info || [];

    if (!inputData.length || !currentReports.length) {
        updateReminderCount();
        return;
    }

    matchedReports = [];

    // 遍历所有报告和输入数据，找到匹配项
    currentReports.forEach(report => {
        inputData.forEach(inputItem => {
            if (inputItem.dataCode && report.dataCode &&
                inputItem.dataCode.toString() === report.dataCode.toString()) {

                // 添加匹配的报告
                matchedReports.push({
                    ...report,
                    matchedOrderId: inputItem.orderId,
                    reportType: currentReportType
                });
            }
        });
    });

    // 更新提醒数量
    updateReminderCount();

    // 如果有匹配的报告，显示提醒
    if (matchedReports.length > 0) {
        showToast(`发现 ${matchedReports.length} 个需要互认的报告`, 'info');
    }
}

// 更新提醒数量
function updateReminderCount() {
    const reminderCount = document.getElementById('reminderCount');
    if (!reminderCount) return;

    reminderCount.textContent = matchedReports.length;

    // 如果没有匹配的报告，隐藏徽章
    if (matchedReports.length === 0) {
        reminderCount.style.display = 'none';
    } else {
        reminderCount.style.display = 'flex';
    }
}

// 渲染提醒列表
function renderReminderList() {
    const reminderList = document.getElementById('reminderList');
    if (!reminderList) return;

    if (matchedReports.length === 0) {
        reminderList.innerHTML = '<div class="no-reminders">暂无匹配的报告</div>';
        return;
    }

    let html = '';

    matchedReports.forEach(report => {
        const reportTime = report.reportType === 'lab' ?
            formatDateTime(report.testReportDate) :
            formatDateTime(report.checkReportDate);

        const projectName = report.reportType === 'lab' ?
            report.testProjCategoryName :
            report.checkProjNameExp;

        html += `
            <div class="reminder-item" data-reportid="${report.id}" data-type="${report.reportType}">
                <div class="reminder-hospital">${report.orgName || '未知医院'}</div>
                <div class="reminder-project">${projectName}</div>
                <div class="reminder-time">${reportTime}</div>
            </div>
        `;
    });

    reminderList.innerHTML = html;

    // 添加点击事件
    const reminderItems = reminderList.querySelectorAll('.reminder-item');
    reminderItems.forEach(item => {
        item.addEventListener('click', function () {
            const reportId = this.dataset.reportid;
            const reportType = this.dataset.type;

            // 如果当前报告类型不同，先切换类型
            if (reportType !== currentReportType) {
                switchReportType(reportType);
            }

            // 高亮并加载报告
            setTimeout(() => {
                highlightAndLoadReport(reportId);
            }, 500);

            // 关闭弹窗
            document.getElementById('reminderPopup').style.display = 'none';
        });
    });
}