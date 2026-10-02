let currentPeriodId = null;
let calendar = null;

document.addEventListener('DOMContentLoaded', function () {
    StartCalendar();

});

const calendarEl = document.getElementById('calendar')

function StartCalendar() {
    fetch('https://overplay-patriarch-daffodil.ngrok-free.dev/api/Build/calendar', { headers: { 'ngrok-skip-browser-warning': 'true' } })
        .then(response => {
            if (!response.ok) {
                throw new Error('データの取得に失敗したよ');
            }
            return response.json();
        })
        .then(data => {
            console.log("C#から届いた生データはこれだ！:", data);
            const year = data.year;
            const month = data.month;
            currentPeriodId = data.id;
            const startMonthStr = String(month).padStart(2, '0');
            const startDateStr = `${year}-${startMonthStr}-01`;
            const lastDay = new Date(year, month + 1, 0).getDate();
            const endDateStr = `${year}-${startMonthStr}-${lastDay}`;
            console.log("C#から届いた生データはこれだ！:", startDateStr);
            InitCalendar(startDateStr, endDateStr);
        })
}
function InitCalendar(start, end) {

    calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',//何週間か月のカレンダーにするかを決めれる
        locale: 'ja',//言語
        initialDate: start,
        // カレンダー上部のヘッダー設定
        customButtons: {//この下に書いたやつは全部ボタンになる
            myCustomButton: {
                text: 'カレンダー作成',
                click: function () {
                    const button = document.querySelector('.fc-myCustomButton-button');//いま画面にあるボタンの要素を取得する
                    if (button) {//そもそもbuttonが画面上にない可能性があるからifをしている
                        if (button.innerText === 'カレンダー作成') {
                            const carendDate = calendar.getDate();
                            const carendYear = carendDate.getFullYear();
                            const carendMonth = carendDate.getMonth() + 1;
                            CreatePeriods(carendYear, carendMonth)
                            alert('まだ作ってないよ');
                        } else if (button.innerText === 'カレンダー編集') {//今は配信中に変えるだけだけどその他の機能を思いついたらここに追加
                            if (currentPeriodId) {
                                UpdateStatus(currentPeriodId);
                            }
                        } else if (button.innerText === 'カレンダー配信中') {
                            alert('今配信中');

                            // fetch('https://overplay-patriarch-daffodil.ngrok-free.dev/api/Build/AgainCalendar')

                        }
                    }

                }
            }
        },


        headerToolbar: {
            left: 'prev,next today', // 左側に「前月」「翌月」「今日」ボタン
            center: 'title',         // 中央に「2026年7月」などのタイトル
            right: 'myCustomButton'
        },
        datesSet: function (info) {
            const carendDate = calendar.getDate();
            const carendYear = carendDate.getFullYear();
            const carendMonth = carendDate.getMonth() + 1;
            console.log(carendYear, carendMonth);
            GetCalendar(carendYear, carendMonth);
        },
        contentHeight: 'auto',
        selectable: true,//カレンダーのマス目をたプできるようにする
    });

    const Btn = document.querySelector('#event-settings-button')//イベント追加ボタンの処理
    const modal = document.querySelector('#my-modal')
    const modalBtn = document.querySelector('#modal-button')
    const modalbodyBtn = document.querySelector('#modal-body-button')
    const modalcalend = document.querySelector('#event-calend')

    if (Btn) {
        Btn.addEventListener('click', function () {
            modal.showModal();
        })
    }
    if (modalBtn) {
        modalBtn.addEventListener('click', function () {
            modal.close();
        })
    }
    if (modalcalend) {//イベントの追加の時今表示しているカレンダーの日付を取るための処理

        InputEventCalend()
    }

    if (modalbodyBtn) {
        modalbodyBtn.addEventListener('click', function () {
            const inputText = document.getElementById('modal-body-text').value;
            const selectedDate = document.getElementById('event-calend').value;

            if (inputText.trim() === "") {
                alert("テキストを入力してください");
                return;
            }
            if (!selectedDate) {
                return;
            }

            const dateObj = new Date(selectedDate);//ばらすためにオブジェクト化する
            const Day = dateObj.getDate();
            const carendDate = calendar.getDate();
            const carendYear = carendDate.getFullYear();
            const carendMonth = carendDate.getMonth() + 1;
            const inputContent = "実験";
            fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/Build/bullidcalender?Getyear=${carendYear}&Getmonth=${carendMonth}`, { headers: { 'ngrok-skip-browser-warning': 'true' } })
                .then(response => {
                    if (!response.ok) {
                        throw new Error('データの取得に失敗したよ');
                    }
                    return response.json();
                })
                .then(data => {

                    fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/Build/modalbodyBtn?GetId=${data.id}&GetDay=${Day}&GetText=${inputText}&GetContent=${inputContent}`, { headers: { 'ngrok-skip-browser-warning': 'true' } })
                        .then(response => {
                            if (!response.ok) {
                                throw new Error('データの取得に失敗したよ');
                            } else {
                                GetEvent(data.id);
                                return response.json();
                            }

                        })
                })
        }

        )
    }

    calendar.render();//これ最後に表示する

}

function InputEventCalend() {
    const modalcalend = document.querySelector('#event-calend')
    if (modalcalend) {//イベントの追加の時今表示しているカレンダーの日付を取るための処理
        const carendDate = calendar.getDate();
        const Year = carendDate.getFullYear();
        const Month = carendDate.getMonth() + 1;

        const monthStr = String(Month).padStart(2, '0');//カレンダーの形式の変更
        const minDate = `${Year}-${monthStr}-01`//月の開始日のやつを作っている
        const lastDay = new Date(Year, Month, 0).getDate();
        const maxDate = `${Year}-${monthStr}-${String(lastDay).padStart(2, '0')}`;

        const dateInput = document.getElementById('event-calend');
        if (dateInput) {
            dateInput.min = minDate;//最小値
            dateInput.max = maxDate;//最大で選べる日付
            dateInput.value = minDate;//初期値
        }
    }
}


function GetCalendar(Year, Month) {//これはカレンダーを矢印で移動させたときに画面に表示するやつ
    fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/Build/bullidcalender?Getyear=${Year}&Getmonth=${Month}`, { headers: { 'ngrok-skip-browser-warning': 'true' } })
        .then(response => {
            if (!response.ok) {
                throw new Error('データの取得に失敗したよ');
            }
            return response.json();
        })
        .then(data => {
            InputEventCalend()
            const button = document.querySelector('.fc-myCustomButton-button');
            console.log(data);
            if (data.id === null) {
                currentPeriodId = null; // 🎯 未作成月なのでIDをリセット
                console.log("カレンダーIDがnull（未作成）なので、イベントの取得はしません！");
                if (button) {
                    button.innerText = 'カレンダー作成';
                }
                const eventAddBtn = document.querySelector('#event-settings-button');
                if (eventAddBtn) {
                    eventAddBtn.disabled = true;
                }
                const nullevent = [
                    { name: '未作成' }
                ];
                CreateEvent(nullevent);
            } else {
                currentPeriodId = data.id;//どのカレンダーかを区別するための番号
                if (button) {

                    if (data.status === '配信中') {
                        button.innerText = 'カレンダー配信中';

                    } else {
                        button.innerText = 'カレンダー編集';
                    }

                }
                const eventAddBtn = document.querySelector('#event-settings-button');
                if (eventAddBtn) {
                    eventAddBtn.disabled = false;
                }
                GetEvent(data.id)
            }
        })
}

function GetEvent(Id) {//カレンダーのidからイベントを探す
    fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/Build/event?GetId=${Id}`, { headers: { 'ngrok-skip-browser-warning': 'true' } })
        .then(response => {
            if (!response.ok) {
                // console.log(response);
                throw new Error(`エラー:${response.status}`);
            }
            return response.json(); // 正常なときだけここにたどり着く
        })
        .then(date => {
            CreateEvent(date)
        })
}

function CreateEvent(Event) {//イベントの枠を作成
    const eventList = document.getElementById('event-list')
    eventList.innerHTML = '';//一度中を空にする
    Event.forEach(item => {
        const html = `
        <div class="event-list-mein">
            <div class="event-info">
                <h3>${item.name}</h3>
                <p class="event-day">${item.day}</p>
            </div>
            <button type="button" class="event-ded-button" data-id="${item.id}">削除ボタン</button>

        </div>
        `;//複製しても分かるようにitem.idを付けるidはかぶったらいけないため複製する場合はclassを付ける
        eventList.insertAdjacentHTML('beforeend', html);
    });
}

const eventList = document.getElementById('event-list')
if (eventList) {
    eventList.addEventListener('click', function (e) {
        if (e.target.classList.contains('event-ded-button')) {
            const eventId = e.target.getAttribute('data-id');
            console.log("削除対象のID:", eventId);
            fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/Build/Deleteevent?GetId=${eventId}`, { headers: { 'ngrok-skip-browser-warning': 'true' } })
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`エラー:${response.status}`);
                    }
                    const carendDate = calendar.getDate();
                    const Year = carendDate.getFullYear();
                    const Month = carendDate.getMonth() + 1;
                    GetCalendar(Year, Month)
                })

        }
    })
}

function CreatePeriods(Year, Month) {//カレンダーのidからイベントを探す
    fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/Build/CreatePeriods?GetYear=${Year}&GetMonth=${Month}`, { headers: { 'ngrok-skip-browser-warning': 'true' } })
        .then(response => {
            if (!response.ok) {
                throw new Error(`エラー:${response.status}`);
            }
            return response.json(); // 正常なときだけここにたどり着く
        })
        .then(data => {
            alert(data.message); // 「新しいシフト期間が作成されました。」を表示


            // これにより GetCalendar 内で button.innerText が 'カレンダー編集' に変わり、二重作成を防げます！
            GetCalendar(Year, Month);
        })
        .catch(error => {
            console.error("作成失敗:", error);
            alert("作成に失敗しました");
        })
    // .then(date => {
    //     CreateEvent(date)
    // })
}

function UpdateStatus(id) {
    fetch(`https://overplay-patriarch-daffodil.ngrok-free.dev/api/Build/UpdateStatus?GetId=${id}`, { headers: { 'ngrok-skip-browser-warning': 'true' } })
        .then(response => {
            if (!response.ok) {
                throw new Error(`エラー:${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            const Year = data.year;
            const Month = data.month;
            // try {
            //     refreshCalendar(); //同じ画面だからこのコードにrefresのメゾットがなくても使用はできる中ったら別の方法
            // } catch (e) {
            //     alert("作成に失敗しました");
            //     console.error("refreshCalendarでエラーが発生しました:", e);
            // }
            const channel = new BroadcastChannel('calendar_channel');
            channel.postMessage('refresh');//合言葉みたいな感じなんでもいい
            GetCalendar(Year, Month);
        })
}