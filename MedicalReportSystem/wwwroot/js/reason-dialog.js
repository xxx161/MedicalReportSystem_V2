// reason-dialog.js

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
    { code: "98", name: "医院未收到或患者未能提供外院检查影像资料无法进行诊断的（该原因不计入【不互认】)" },
    { code: "99", name: "由于疾病诊断或病情发展监测需要进行连续性检查/检验（该原因不计入【不互认】)" }
];

// 显示原因选择弹窗
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