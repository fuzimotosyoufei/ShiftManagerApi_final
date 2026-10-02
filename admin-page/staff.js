let JOB_MASTER = [];
const ROLE_MASTER = ['正社員', '準社員', 'パート'];

// --------------------------------------------------
// 共通処理：APIからのデータをIDごとにグループ化し、jobsを配列にまとめる関数
// --------------------------------------------------
function formatStaffData(rawData) {
    if (!rawData || !Array.isArray(rawData)) return [];

    return rawData.reduce((acc, current) => {
        const existingStaff = acc.find(item => item.id === current.id);
        if (existingStaff) {
            if (current.job_name && !existingStaff.jobs.includes(current.job_name)) {
                existingStaff.jobs.push(current.job_name);
            }
        } else {
            acc.push({
                id: current.id,
                name: current.staff_name || current.name || '',
                role: current.role || '',
                line_id: current.line_id || '',
                status: current.status || '',
                jobs: current.job_name ? [current.job_name] : []
            });
        }
        return acc;
    }, []);
}

async function fetchJobMaster() {
    fetch('https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/joblist',{
        method: 'GET',
            headers: {

            'ngrok-skip-browser-warning': 'true'
        }
    })
    .then(response =>{
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        // 💡 レスポンス本文をJSONオブジェクトとして解析
        return response.json();
    })
    .then(data=>{
        // alert(data);
       JOB_MASTER =  data;
    })
}
// 現在「編集モード」かどうかを管理するフラグ
let isEditMode = false;

// 画面表示関数（引数でモードを判定）
function showStaffList() {
    const listEl = document.getElementById('staff-list');
    if (!listEl) return;
    listEl.innerHTML = '';
    fetch('https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/stafflist', {
        method: 'GET',
        headers: {

            'ngrok-skip-browser-warning': 'true'
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            // 💡 レスポンス本文をJSONオブジェクトとして解析
            return response.json();
        })
        .then(data => {
            // 💡 共通関数でデータを整形
            const groupedStaffs = formatStaffData(data);

        // ② 描画処理
        groupedStaffs.forEach(staff => {
            const li = document.createElement('li');
            li.className = 'staff-card';

            if (!isEditMode) {
                // --------------------------------------------------
                // A. 通常の閲覧モード（テキスト表示）
                // --------------------------------------------------
                const jobBadges = staff.jobs
                    .map(job => `<span class="job-badge">${job}</span>`)
                    .join(' ');

                li.innerHTML = `
                <div class="staff-header">
                    <span class="staff-id">ID: ${staff.id}</span>
                    <strong class="staff-name">${staff.name}</strong>
                    <span class="role-badge">${staff.role}</span>
                </div>
                <div class="staff-jobs">
                    <span class="job-label">担当職種：</span>${jobBadges}
                </div>
            `;
            } else {
                // --------------------------------------------------
                // B. 編集モード（フォーム・プルダウン表示）
                // --------------------------------------------------
                const roleOptions = ROLE_MASTER.map(role =>
                    `<option value="${role}" ${role === staff.role ? 'selected' : ''}>${role}</option>`
                ).join('');

                const addJobOptions = JOB_MASTER//回して職種を追加している
                    .map(job => `<option value="${job}">${job}</option>`)
                    .join('') + `<option value="__NEW__">＋ 新しい職種を追加...</option>`;//joinを消すことでカンマが消えて綺麗になる

                // 登録中の職種（×ボタン付きバッジ）
                const jobBadges = staff.jobs.map(job => `
                    <span class="edit-job-badge">
                        ${job}
                        <button type="button" class="btn-delete-job" onclick="deleteJobFromStaff(${staff.id}, '${job}', this)">×</button>
                    </span>
                `).join('');

                li.innerHTML = `
                    <div class="edit-staff-form" data-id="${staff.id}">
                        <div class="edit-row">
                            <span class="staff-id">ID: ${staff.id}</span>
                            
                            <!-- 名前変更インプット -->
                            <input type="text" class="edit-input-name" value="${staff.name}" placeholder="名前">

                            <button type="button" class="btn-change-name" onclick="addchangename('${staff.id}', this)">名前変更</button>
                          
                            <!-- 区分プルダウン -->
                            <select class="edit-select-role" onchange="updateRole(${staff.id},this.value)">
                                ${roleOptions}
                            </select>
                             <button type="button" class="btn-staff-delete" onclick="deleteStaff('${staff.id}', '${staff.name}')">スタッフを削除</button>
                        </div>

                        <div class="edit-row-jobs">
                            <span class="job-label">担当職種：</span>
                            <div class="edit-job-list" id="job-container-${staff.id}">
                                ${jobBadges}
                            </div>
                        </div>

                        <!-- 職種追加プルダウン -->
                        <div class="add-job-area">
                            <select class="add-job-select" id="add-job-select-${staff.id}" onchange="InJob('${staff.id}')">
                                <option value="" disabled selected>＋ 職種を追加...</option>
                                ${addJobOptions}
                            </select>
                            <button type="button" class="btn-add-job" onclick="addJobToStaff('${staff.id}')">追加</button>
                          
                    </div>
                `;
            }

            listEl.appendChild(li);
        });
        })
        .catch(error => {
            console.error('データ取得エラー:', error);
        });
}
function deleteStaff(staffId, staffName){
    const result = window.confirm(`${staffName}'さんを本当に削除しますか？`);

    if (result) {
        fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/deletestaff?staffId=${staffId}`, {
            method: 'POST',
            headers: {
                'ngrok-skip-browser-warning': 'true'
            }
        })
            .then(response => {
                if (!response.ok) throw new Error('削除に失敗しました');
                return response.json();
            })
            .then(data => {
                alert(data.message || 'スタッフを削除しました');
                showStaffList();
            })
            .catch(error => {
                console.error('職種追加エラー:', error);
                alert('職種の追加に失敗しました。');
            });
        console.log("削除を実行しました");
        // ここにDB更新やAPI呼び出しの処理を書く
    } else {
        // キャンセルされた場合
        console.log("削除をキャンセルしました");
    }
}
async function addchangename(staffId, buttonEl) {
    const inputEl = buttonEl.parentElement.querySelector('.edit-input-name');
    const newName = inputEl ? inputEl.value : '';

    if (!newName.trim()) {
        alert('名前を入力してください。');
        return;
    }

    // 💡 try...catch でエラーハンドリング
    try {
        const response = await fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/namechangename?staffId=${staffId}&newName=${encodeURIComponent(newName)}`, {
            method: 'GET',
            headers: {
                'ngrok-skip-browser-warning': 'true'
            }
        });

        if (!response.ok) throw new Error('名前変更に失敗しました');

        const data = await response.json();

        alert(data.message);

        // 成功したら一覧を再描画
        if (typeof showStaffList === 'function') {
            showStaffList();
        }

    } catch (error) {
        console.error('名前変更エラー:', error);
        alert('名前の変更に失敗しました。');
    }
}



// --------------------------------------------------
// 職種を動的に追加する関数
// --------------------------------------------------
async function addJobToStaff(staffId) {
        const selectEl = document.getElementById(`add-job-select-${staffId}`);
        if (!selectEl) return;
        const selectedJob = selectEl.value;

        if (!selectedJob || selectedJob === '__NEW__') return;

        const container = document.getElementById(`job-container-${staffId}`);

        // 1. 重複チェック（既存のバッジテキスト内に選択された職種名があるか）
        const existingBadges = Array.from(container.querySelectorAll('.edit-job-badge'));
        const exists = existingBadges.some(el => el.textContent.includes(selectedJob));
        if (exists) {
            alert('すでに存在する職種です。');
            return;
        }


        // 2. API（POST）通信
        fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/injob?staffId=${staffId}&jobname=${encodeURIComponent(selectedJob)}`, {
            method: 'POST',
            headers: {
                'ngrok-skip-browser-warning': 'true'
            }
        })
            .then(response => {
                if (!response.ok) throw new Error('追加に失敗しました');
                return response.json();
            })
            .then(data => {
                // 3. DB追加成功後に、×ボタン付きバッジ要素を作成してDOMに追加
                const newBadge = document.createElement('span');
                newBadge.className = 'edit-job-badge';
                newBadge.innerHTML = `
                ${selectedJob}
                <button type="button" class="btn-delete-job" onclick="deleteJobFromStaff(${staffId}, '${selectedJob}', this)">×</button>
            `;

                container.appendChild(newBadge);
                
                // 選択肢（プルダウン）を初期状態に戻す
                selectEl.selectedIndex = 0;
            })
            .catch(error => {
                console.error('職種追加エラー:', error);
                alert('職種の追加に失敗しました。');
            });
    }
async function InJob(staffId) {//新しい職種を追加する処理
    const selectEl = document.getElementById(`add-job-select-${staffId}`);
    if (!selectEl) return;
    let selectedJob = selectEl.value;
    if (!selectedJob) return;


    // 💡 「＋ 新しい職種を追加...」が選択された場合の処理
    if (selectedJob === '__NEW__') {
        const newJobName = prompt('新しい職種名を入力してください：');

        // キャンセルされたか、未入力の場合は元に戻す
        if (!newJobName || !newJobName.trim()) {
            selectEl.selectedIndex = 0;
            return;
        }

        const trimmedJobName = newJobName.trim();

        try {
            const response = await fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/injobmaster?jobname=${encodeURIComponent(trimmedJobName)}`, {
                method: 'GET',
                headers: { 'ngrok-skip-browser-warning': 'true' }
            });

            if (!response.ok) throw new Error('マスター追加に失敗しました');
            const data = response.json();
            JOB_MASTER.push(trimmedJobName);
            selectedJob = trimmedJobName;
            alert(data.message);
            showStaffList();
        } catch (e) {
            console.error('エラー詳細:', e);
            selectEl.selectedIndex = 0;
            return;
        }


    }
}

function updateRole(staffId,rolName){
    fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/updaterole?staffId=${staffId}&rolename=${encodeURIComponent(rolName)}`, {
        method: 'GET',
        headers: {
            'ngrok-skip-browser-warning': 'true'
        }
    })
        .then(response => {
            if (!response.ok) throw new Error('ロールの更新に失敗しました');
            return response.json();
        })
        .then(data => {
            alert(data.message || '更新が完了しました');
            // DB削除成功後に画面からバッジを取り除く
        })
        .catch(error => {
            console.error('職種削除エラー:', error);
            alert('職種の削除に失敗しました。');
        });
}
function deleteJobFromStaff(staffId, jobName, buttonEl) {//buttonELの認識は押されたバツから一番近い枠削除するために使う職種の削除処理
    if (!confirm(`「${jobName}」を削除しますか？`)) return;

    fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/deljob?staffId=${staffId}&jobname=${encodeURIComponent(jobName)}`, {
        method: 'GET',
        headers: {
            'ngrok-skip-browser-warning': 'true'
        }
    })
        .then(response => {
            if (!response.ok) throw new Error('削除に失敗しました');
            return response.json();
        })
        .then(data => {
            alert(data.message || '削除しました');
            // DB削除成功後に画面からバッジを取り除く
            const badgeEl = buttonEl.closest('.edit-job-badge');
            if (badgeEl) badgeEl.remove();
        })
        .catch(error => {
            console.error('職種削除エラー:', error);
            alert('職種の削除に失敗しました。');
        });
}
// モーダルを開く処理
function OpenStaff() {
    const modal = document.querySelector('#my-staff-modal');
    if (!modal) return;

    const roleSelect = document.querySelector('#modal-body-role-select');
    roleSelect.innerHTML = '<option value="" disabled selected>選択してください</option>' +
        ROLE_MASTER.map(role => `<option value="${role}">${role}</option>`).join('');
    const jobSelect = document.querySelector('#modal-body-job-select');
    jobSelect.innerHTML = `<option value="" disabled selected>選択してください</option>` +
        JOB_MASTER.map(job => `<option value="${job}">${job}</option>`).join('');

    modal.showModal(); // これで背景が暗くなる標準モーダルが開く
}

// モーダルを閉じる処理
function CloseStaff() {
    const modal = document.querySelector('#my-staff-modal');
    if (modal) modal.close(); // これでモーダルが閉じる
}
function InsertStaff() {
    const name = document.querySelector('#modal-body-name-text').value;
    const role = document.querySelector('#modal-body-role-select').value;
    const job = document.querySelector('#modal-body-job-select').value;
    const modal = document.querySelector('#my-staff-modal');

    if (!name || !role || !job) {
        alert('すべての項目を入力してください。');
        return;
    }
    
    fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/inManualstaff?name=${encodeURIComponent(name)}&line_id=${"null"}&role=${encodeURIComponent(role)}&job=${encodeURIComponent(job)}`, {
        method: 'GET',
        headers: {
            'ngrok-skip-browser-warning': 'true'
        }
    })
        .then(response => {
            if (!response.ok) throw new Error('削除に失敗しました');
            return response.json();
        })
        .then(data => {
            alert(data.message || '登録が完了しました');
            if (modal) modal.close();
            showStaffList();
            // DB削除成功後に画面からバッジを取り除く
        })
        .catch(error => {
            console.error('登録失敗しましたエラー:', error);
            alert('スタッフの登録に失敗しました。');
        });
}
// --------------------------------------------------
// 職種マスター削除モーダル関連処理
// --------------------------------------------------

// モーダルを開く
function OpenJobDelete() {
    const dialog = document.getElementById('my-job-modal');
    updateJobMasterSelectOptions(); // セレクトボックスを最新にする
    if (dialog) dialog.showModal();
}

// モーダルを閉じる
function CloseJobDelete() {
    const dialog = document.getElementById('my-job-modal');
    if (dialog) dialog.close();
}

// モーダル内の削除対象プルダウンを更新する関数
function updateJobMasterSelectOptions() {
    const selectEl = document.getElementById('delete-master-job-select');
    if (!selectEl) return;

    let html = '<option value="" disabled selected>選択してください...</option>';
    if (typeof JOB_MASTER !== 'undefined' && Array.isArray(JOB_MASTER)) {
        JOB_MASTER.forEach(job => {
            html += `<option value="${job}">${job}</option>`;
        });
    }
    selectEl.innerHTML = html;
}

// 選択した職種をマスターから削除する関数
async function deleteJobMasterFromModal() {
    const selectEl = document.getElementById('delete-master-job-select');
    if (!selectEl) return;

    const jobName = selectEl.value;
    if (!jobName) {
        alert('削除する職種を選択してください。');
        return;
    }

    if (!confirm(`「${jobName}」を職種マスターから削除しますか？\n（※全ての選択肢から除去されます）`)) {
        return;
    }

    try {
        const response = await fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/deljobmaster?jobname=${encodeURIComponent(jobName)}`, {
            method: 'GET',
            headers: {
                'ngrok-skip-browser-warning': 'true'
            }
        });

        if (!response.ok) throw new Error('削除に失敗しました');

        const data = await response.json();

        if (data.message === '全てのスタッフから削除される職種をなくしてください') {
            alert(data.message);
            return;
        }

        // メモリ上のマスター配列から削除
        if (typeof JOB_MASTER !== 'undefined') {
            JOB_MASTER = JOB_MASTER.filter(j => j !== jobName);
        }

        alert(`「${jobName}」を職種マスターから削除しました。`);

        // モーダル内のプルダウン更新
        updateJobMasterSelectOptions();

        // スタッフ一覧全体のプルダウンを再描画
        if (typeof showStaffList === 'function') {
            showStaffList();
        }

    } catch (error) {
        console.error('職種マスター削除エラー:', error);
        alert('職種の削除に失敗しました。');
    }
}
function switchTab(tabId, button) {
    // すべてのタブコンテンツを非表示にする
    document.querySelectorAll('.tab-content').forEach(tab => {
        tab.classList.remove('active');
    });
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    // 指定されたタブコンテンツを表示する
    const targetTab = document.getElementById(tabId);
    if (targetTab) targetTab.classList.add('active');
    // 選択されたタブボタンにactiveクラスを追加
    button.classList.add('active');
    if(tabId === 'content-tab2') {
        StaffApplications();    
    }
}
function StaffApplications() {
   fetch('https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/staffapplicationlist', {
       method: 'GET',
       headers: {
           'ngrok-skip-browser-warning': 'true'
       }
   })
       .then(response => {
           if (!response.ok) throw new Error('表示に');
           return response.json();
       })
       .then(data => {
           if (data.length > 0) alert(data[0].id);
           StaffApplicationsList(data);
    
           // DB削除成功後に画面からバッジを取り除く
       })
       .catch(error => {
           console.error('登録失敗しましたエラー:', error);
           alert('スタッフの登録に失敗しました。');
       });

    //    .then(data => {
    //        // 💡 配列なので、最初の1件目を表示する場合は [0] をつけます
    //        if (data.length > 0) {
    //            alert(`1件目のID: ${data[0].id} / 名前: ${data[0].staff_name}`);
    //        } else {
    //            alert('データが0件でした');
    //        }

    //        // 💡 取得したデータ（配列）を関数に渡す
    //        showStaffList(data);
    //    })
}
function StaffApplicationsList(data) {
    const listEl = document.getElementById('staff-application-list');
    if (!listEl) return;

    listEl.innerHTML = '';

    // 💡 共通関数を使って rawData を整形し、jobs 配列を自動生成する
    const groupedStaffs = formatStaffData(data);

    // 2. データが空の場合の表示（親切設計）
    if (!groupedStaffs || groupedStaffs.length === 0) {
        listEl.innerHTML = '<p>現在、申請はありません。</p>';
        return;
    }
    
    groupedStaffs.forEach(staff => {
        const li = document.createElement('li');
        li.style.listStyle = 'none';
        const roleOptions = ROLE_MASTER.map(role =>
            `<option value="${role}" ${role === staff.role ? 'selected' : ''}>${role}</option>`
        ).join('');

        const addJobOptions = JOB_MASTER//回して職種を追加している
            .map(job => `<option value="${job}">${job}</option>`)
            .join('') + `<option value="__NEW__">＋ 新しい職種を追加...</option>`;//joinを消すことでカンマが消えて綺麗になる

        // 登録中の職種（×ボタン付きバッジ）
        const jobBadges = staff.jobs.map(job => `
                    <span class="edit-job-badge">
                        ${job}
                        <button type="button" class="btn-delete-job" onclick="deleteJobFromStaff(${staff.id}, '${job}', this)">×</button>
                    </span>
                `).join('');
        li.innerHTML = `
            <div class="staff-Application-form" data-id="${staff.id}">
                <div class="staff-Application-row">
                    <strong class="staff-Application-name">${staff.name}</strong>
                    <span class="role-Application-badge">${staff.status}</span>
                    <!-- 区分プルダウン -->
                    <select class="edit-select-role" onchange="updateRole('${staff.id}', this.value)">
                        ${roleOptions}
                    </select>
                   
                </div>

                <!-- 職種選択と申請ボタン領域 -->
                <div class="add-job-area">
                    <label for="add-job-select-${staff.id}">担当職種：</label>
                    <select class="add-job-select" id="add-job-select-${staff.id}" onchange="InJob('${staff.id}')">
                        <option value="" disabled selected>＋ 職种を選択...</option>
                        ${addJobOptions}
                    </select>

                    <!-- 1: 承諾 / 2: 却下 -->
                    <button type="button" class="btn-add-job" onclick="staffApplicationsConsent('${staff.id}', '${staff.line_id}','${staff.name}','${staff.status}',1)">申請を承諾</button>
                    <button type="button" class="btn-add-job" onclick="staffApplicationsConsent('${staff.id}', '${staff.line_id}','${staff.name}','${staff.status}',2)">申請を却下</button>
                </div>
            </div>
    `;
listEl.appendChild(li);
        // showStaffList();
})
}
function staffApplicationsConsent(staffId, lineId, name, status, count) {
    //役職選んでないとはじくように設定して
    //役職を送る方法を考えて
    if (count  === 1) {
        // 承諾処理（プルダウンで選ばれた職種を追加して申請を完了）
        const selectedJob = document.getElementById(`add-job-select-${staffId}`).value;
        const selectedRole = document.querySelector(`.staff-Application-form[data-id="${staffId}"] .edit-select-role`).value;
        if (!selectedJob ) {
            alert('職種を選択してください。');
            return;
        }else{
            console.log(`スタッフID: ${staffId} ${status}の申請を承諾（追加職種: ${selectedJob}）${selectedRole}`);
            fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/inManualstaff?name=${encodeURIComponent(name)}&line_id=${encodeURIComponent(lineId)}&role=${encodeURIComponent(selectedRole)}&job=${encodeURIComponent(selectedJob)}`, {
                method: 'GET',
                headers: {
                    'ngrok-skip-browser-warning': 'true'
                }
            })
                .then(response => {
                    if (!response.ok) throw new Error('削除に失敗しました');
                    return response.json();
                })
                .then(data => {
                    alert(data.message || '登録が完了しました');
                    staffApplicationsCheck(staffId, count);
                    // DB削除成功後に画面からバッジを取り除く
                })
                .catch(error => {
                    console.error('登録失敗しましたエラー:', error);
                    alert('スタッフの登録に失敗しました。');
                });
        }
        
    } else if (count === 2) {
        // 却下処理（何も追加せずに申請を却下）
        console.log(`スタッフID: ${staffId} ${status}の申請を却下`);
        staffApplicationsCheck(staffId, count);
    }
}
function staffApplicationsCheck(staffId, count) {
    fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/staff/staffapplicationsCheck?id=${encodeURIComponent(staffId)}&count=${encodeURIComponent(count)}`, {
        method: 'GET',
        headers: {
            'ngrok-skip-browser-warning': 'true'
        }
    })
        .then(response => {
            if (!response.ok) throw new Error('チェックに失敗しました');
            return response.json();
        })
        .then(data => {
            StaffApplications();
            // DB削除成功後に画面からバッジを取り除く
        })
        .catch(error => {
            console.error('チェック失敗しましたエラー:', error);
        });
}

// --------------------------------------------------
// イベント設定：編集ボタン押下でモード切替
// --------------------------------------------------
document.addEventListener('DOMContentLoaded', () => {
    fetchJobMaster()
    const editBtn = document.getElementById('staff-Edit-button');
    const deleteBtn = document.getElementById('job-Master-button');
    if (editBtn) {
        editBtn.addEventListener('click', () => {
            // モードを反転
            isEditMode = !isEditMode;

            if (isEditMode) {
                editBtn.textContent = 'スタッフ編集終了';
                editBtn.classList.add('editing');
                deleteBtn.classList.add('editing'); // 削除ボタンを表示
            } else {
                editBtn.textContent = 'スタッフ編集';
                editBtn.classList.remove('editing');
                deleteBtn.classList.remove('editing');
                // ※ここでC# APIへUPDATE処理を呼び出す処理を接続できます
            }

            // 再描画
            showStaffList();
        });
    }

    // 初回描画
    showStaffList();
});



// 2. 画面にリストを表示する関数
// function showStaffList(data) {
//     const listEl = document.getElementById('staff-list');
//     listEl.innerHTML = '';

//     // ① IDごとにデータをグループ化（職種を配列にまとめる）
//     const groupedStaffs = data.reduce((acc, current) => {//reduceが複数の中から集約したやつと今からのやつを比較しaccにはまとめたデータcurrentには今から追加するデータが入る
//         const existingStaff = acc.find(item => item.id === current.id);//今までのやつと今から取得する関数のidを比較
//         if (existingStaff) {
//             existingStaff.jobs.push(current.job_name);//あるならここで同じ配列に追加詳しく調べてもいいよ
//         } else {
//             acc.push({
//                 id: current.id,
//                 name: current.staff_name,
//                 role: current.role,
//                 jobs: [current.job_name] // 配列として保持
//             });
//         }
//         return acc;
//     }, []);

//     // ② DOMの生成
//     groupedStaffs.forEach(staff => {
//         const li = document.createElement('li');
//         li.className = 'staff-card';

//         // 複数の職種をカンマ区切り、またはバッジ形式でまとめる
//         const jobBadges = staff.jobs
//             .map(job => `<span class="job-badge">${job}</span>`)
//             .join(' ');

//         li.innerHTML = `
//             <div class="staff-header">
//                 <span class="staff-id">ID: ${staff.id}</span>
//                 <strong class="staff-name">${staff.name}</strong>
//                 <span class="role-badge">${staff.role}</span>
//             </div>
//             <div class="staff-jobs">
//                 <span class="job-label">担当職種：</span>${jobBadges}
//             </div>
//             <div>
                
//             </div>
//         `;
//         listEl.appendChild(li);
//     });
// }
// document.addEventListener('DOMContentLoaded', () => {
//     showStaffList(rawDataFromDb);
// });
// 実行